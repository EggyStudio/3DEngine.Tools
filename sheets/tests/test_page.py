# The page a stranger reads first is true of the scripts as they are: it opens with a picture and
# the three lines that make it, shows each layout word for word beside the picture it draws, and
# every layout draws.
import glob, json, os, unittest

from tests.pictures import FOLDER, Made, needs_pillow, run

EXAMPLES = os.path.join(FOLDER, "examples")


def read(path):
    with open(path, encoding="utf-8") as f:
        return f.read().replace("\r\n", "\n")


class PageTests(unittest.TestCase):

    def test_the_page_opens_with_a_picture_and_the_three_lines_that_make_it(self):
        page = read(os.path.join(FOLDER, "README.md")).split("\n")
        first = read(os.path.join(EXAMPLES, "first.json")).strip()

        self.assertTrue(page[0].startswith("# "))
        picture = next(i for i, line in enumerate(page) if line and not line.startswith("# "))
        self.assertEqual("![A first chart](examples/first.webp)", page[picture])
        block = [line for line in page[picture + 1:] if line][:5]
        self.assertEqual(["```bash", "pip install pillow", f"echo '{first}' > first.json",
                          "python3 -I chart.py first.json first.webp", "```"], block)
        self.assertTrue(os.path.exists(os.path.join(EXAMPLES, "first.webp")), "first.webp is not drawn")

    def test_the_page_shows_each_layout_word_for_word_beside_its_picture(self):
        page = read(os.path.join(FOLDER, "README.md"))
        # The first is shown in the three lines the page opens with, which the test above reads.
        layouts = sorted(p for p in glob.glob(os.path.join(EXAMPLES, "*.json")) if not p.endswith("first.json"))
        self.assertTrue(layouts)
        for layout in layouts:
            name = os.path.splitext(os.path.basename(layout))[0]
            with self.subTest(name):
                script = "sheet.py" if "rows" in json.loads(read(layout)) else "chart.py"
                self.assertIn(f"](examples/{name}.webp)", page)
                self.assertTrue(os.path.exists(os.path.join(EXAMPLES, name + ".webp")), f"{name}.webp is not drawn")
                self.assertIn(f"python3 -I {script} examples/{name}.json examples/{name}.webp", page)
                self.assertIn(f"```json\n{read(layout).rstrip()}\n```", page)

    @needs_pillow
    def test_every_layout_of_the_examples_draws_with_its_script(self):
        with Made() as made:
            for layout in sorted(glob.glob(os.path.join(EXAMPLES, "*.json"))):
                # A sheet of captures is laid out by its rows, a sheet of charts by its panels.
                script = "sheet.py" if "rows" in json.loads(read(layout)) else "chart.py"
                name = os.path.splitext(os.path.basename(layout))[0]
                with self.subTest(name):
                    printed = "\n".join(run(script, layout, made / (name + ".png")))
                    self.assertIn("panel", printed)
