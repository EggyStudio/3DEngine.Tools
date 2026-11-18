# sheet.py places its panels on white, reads its boxes and sets its titles where it says it does.
import json, unittest

from tests.pictures import Made, Picture, needs_pillow, run, single, write_pfm, write_ppm

WIDTH, HEIGHT = 64, 32


def line(lines, kind):
    """A row's title or labels, the top of its line and its height."""
    words = single(lines, f"row 1 {kind} at ").split(" ")
    return int(words[4].split(",")[1]), int(words[6])


def place(lines, panel):
    x, y = single(lines, f"row 1 panel {panel} at ").split(" ")[5].split(",")[:2]
    return int(x), int(y)


@needs_pillow
class SheetTests(unittest.TestCase):

    def test_a_sheet_places_its_panels_on_white_and_reads_its_boxes(self):
        with Made() as made:
            # A frame at 0.25 with a square at 0.5 on the left and one at 0.125 in the middle, its
            # reference at 0.25 throughout, and two pictures, a square of the second 10 lighter.
            write_pfm(made / "frame.pfm", WIDTH, HEIGHT,
                      lambda x, y: 0.5 if x < 16 and y < 16 else 0.125 if 32 <= x < 48 and y < 16 else 0.25)
            write_pfm(made / "reference.pfm", WIDTH, HEIGHT, lambda x, y: 0.25)
            write_ppm(made / "first.ppm", WIDTH, HEIGHT, lambda x, y: 128)
            write_ppm(made / "second.ppm", WIDTH, HEIGHT, lambda x, y: 138 if x < 16 and y < 16 else 128)
            layout = {
                "font": 12,
                "columns": ["frame", "frame less reference", "two pictures"],
                "rows": [{
                    "title": "One row",
                    "panels": [
                        {"image": "frame.pfm", "boxes": [[0, 0, 16, 16], [8, 0, 24, 16]]},
                        {"signed": ["frame.pfm", "reference.pfm"], "share": 0.5},
                        {"difference": ["first.ppm", "second.ppm"], "times": 4},
                    ],
                }],
            }
            with open(made / "layout.json", "w", encoding="utf-8") as f:
                json.dump(layout, f)

            lines = run("sheet.py", made / "layout.json", made / "sheet.ppm")

            # The first box lies on the bright square, the second half on it and half off.
            self.assertIn("row 1 panel 1 box 1 at 0,0,16,16 reads 0.5", lines)
            self.assertIn("row 1 panel 1 box 2 at 8,0,24,16 reads 0.375", lines)
            sheet = Picture(made / "sheet.ppm")
            self.assertEqual((255, 255, 255), sheet.at(0, 0))
            capture, signed, difference = place(lines, 1), place(lines, 2), place(lines, 3)

            # 0.25 of linear light is 137 in sRGB, read away from the boxes and their readings.
            self.assertEqual((137, 137, 137), sheet.at(capture[0] + 56, capture[1] + 28))
            self.assertEqual((255, 0, 0), sheet.at(signed[0] + 8, signed[1] + 8))
            self.assertEqual((0, 0, 255), sheet.at(signed[0] + 40, signed[1] + 8))
            self.assertEqual((215, 215, 215), sheet.at(difference[0] + 8, difference[1] + 8))
            self.assertEqual((255, 255, 255), sheet.at(difference[0] + 40, difference[1] + 20))

            # The signed difference is the sheet's one dark panel, black where the frame meets its
            # reference, which three of its corners do.
            self.assertEqual((0, 0, 0), sheet.at(signed[0] + 56, signed[1] + 28))
            self.assertEqual((0, 0, 0), sheet.at(signed[0] + WIDTH - 1, signed[1]))
            self.assertEqual((0, 0, 0), sheet.at(signed[0], signed[1] + HEIGHT - 1))
            self.assertEqual((0, 0, 0), sheet.at(signed[0] + WIDTH - 1, signed[1] + HEIGHT - 1))

            # Every panel stands on the white ground, left, right and below.
            for x, y in (capture, signed, difference):
                self.assertEqual((255, 255, 255), sheet.at(x - 1, y + HEIGHT // 2))
                self.assertEqual((255, 255, 255), sheet.at(x + WIDTH, y + HEIGHT // 2))
                self.assertEqual((255, 255, 255), sheet.at(x + WIDTH // 2, y + HEIGHT))

            # The row's title stands first, on a line of its own above the panels' labels and in a
            # larger type than theirs.
            title, labels = line(lines, "title"), line(lines, "labels")
            self.assertLessEqual(title[0] + title[1], labels[0], "the title runs into the labels")
            self.assertLessEqual(labels[0] + labels[1], capture[1], "the labels run into the panels")
            self.assertGreater(title[1], labels[1], "the title's line is no higher than the labels'")

    def test_a_sheet_opens_with_its_title_and_subtitle_above_its_first_row(self):
        with Made() as made:
            write_ppm(made / "first.ppm", WIDTH, HEIGHT, lambda x, y: 128)
            layout = {"title": "The sheet's title", "subtitle": "Its subtitle", "font": 12,
                      "rows": [{"title": "One row", "panels": [{"image": "first.ppm"}]}]}
            with open(made / "layout.json", "w", encoding="utf-8") as f:
                json.dump(layout, f)

            lines = run("sheet.py", made / "layout.json", made / "sheet.ppm")

            at = lambda printed: int(printed.split(" ")[3].split(",")[1])
            title, subtitle = at(single(lines, "sheet title at ")), at(single(lines, "sheet subtitle at "))
            self.assertLess(title, subtitle, "the subtitle stands above the title")
            self.assertLess(subtitle, line(lines, "title")[0], "the row's title stands above the sheet's subtitle")
            sheet = Picture(made / "sheet.ppm")
            self.assertTrue(any(sum(sheet.at(x, title + 8)) < 300 for x in range(200)), "the sheet's title is not drawn")
