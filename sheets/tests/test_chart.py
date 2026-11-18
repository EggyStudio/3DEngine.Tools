# chart.py draws each kind of panel where it says it does, its labels clear of their lines and its
# ticks inside the ranges given.
import json, re, unittest

from tests.pictures import Made, Picture, Plot, attempt, needs_pillow, run, single


def draw(made, layout):
    with open(made / "layout.json", "w", encoding="utf-8") as f:
        json.dump(layout, f)
    lines = run("chart.py", made / "layout.json", made / "sheet.ppm")
    return lines, Picture(made / "sheet.ppm")


def numbers(line):
    return [int(n) for n in re.findall(r"-?\d+", line)]


def axes(low=0, high=10):
    return {"min": low, "max": high, "ticks": [low, high]}


@needs_pillow
class ChartTests(unittest.TestCase):

    def test_each_panel_draws_its_values_where_its_printed_place_says(self):
        layout = {
            "title": "A made sheet", "subtitle": "Every kind of panel", "notes": "A note under the panels.",
            "columns": 2, "width": 1200, "panel_height": 360, "font": 14,
            "panels": [
                {"kind": "line", "title": "Linear", "x": axes(), "y": axes(),
                 "bands": [{"from": 7, "to": 9, "label": "a band"}],
                 "series": [
                     {"label": "solid", "color": "#ff0000", "points": [[0.0, 5], [10.0, 5]], "marked": [[3.0, 5]]},
                     {"label": "dashed", "color": "#0000ff", "dash": True, "points": [[0.0, 2], [10.0, 2]]},
                 ]},
                {"kind": "line", "title": "Logarithmic", "x": axes(), "y": {"log": True, "min": 1, "max": 10000},
                 "series": [{"color": "#00ff00", "points": [[0.0, 100], [10.0, 100]]}]},
                {"kind": "bars", "title": "Bars", "x": axes(0, 40),
                 "series": [{"label": "first", "color": "#ff0000"}, {"label": "second", "color": "#00ff00"}],
                 "groups": [{"label": "A", "values": [10, 20]}, {"label": "B", "values": [5, None]}]},
                {"kind": "table", "title": "Table", "columns": ["name", "value"],
                 "rows": [["Same text", "1"], ["Same text", "1"]], "bold": [0]},
                {"kind": "map", "title": "Map", "scale": 1, "values": [[1.0, -1, 0]]},
            ],
        }
        with Made() as made:
            lines, sheet = draw(made, layout)

            self.assertEqual((255, 255, 255), sheet.at(2, 2))
            self.assertGreater(sheet.dark(0, 0, sheet.width, 60), 50, "the title is not drawn")
            self.assertGreater(sheet.dark(0, sheet.height - 50, sheet.width, sheet.height), 20, "the notes are not drawn")

            linear = Plot(lines, 1)
            self.assertEqual((255, 0, 0), sheet.at(linear.x(5), linear.y(5)))
            self.assertEqual((255, 0, 0), sheet.at(linear.x(3), linear.y(5) + 4))
            self.assertEqual((238, 236, 232), sheet.at(linear.x(2), linear.y(8)))
            dashed = [sheet.at(x, linear.y(2)) for x in range(linear.x(1), linear.x(9))]
            self.assertIn((0, 0, 255), dashed)
            self.assertIn((255, 255, 255), dashed)

            log = Plot(lines, 2)
            self.assertTrue(single(lines, "panel 2 plot at ").endswith(" y 1..10,000 log"))
            self.assertEqual((0, 255, 0), sheet.at(log.x(5), log.top + log.height // 2))

            x0, y0, x1, y1 = numbers(single(lines, "panel 3 bar 1 2 at "))[3:7]
            self.assertEqual((0, 255, 0), sheet.at((x0 + x1) // 2, (y0 + y1) // 2))
            self.assertEqual((255, 255, 255), sheet.at(x1 + 3, (y0 + y1) // 2))
            self.assertFalse([line for line in lines if line.startswith("panel 3 bar 2 2 ")])

            bold_line = single(lines, "panel 4 row 1 at ")
            self.assertTrue(bold_line.endswith(" bold"))
            (bx, by, bw, bh), (px, py, pw, ph) = (numbers(bold_line)[2:6], numbers(single(lines, "panel 4 row 2 at "))[2:6])
            self.assertGreater(sheet.dark(bx, by, bx + bw, by + bh - 3), sheet.dark(px, py, px + pw, py + ph - 3),
                               "the bold row is no darker than the same text plain")

            map_plot = Plot(lines, 5)
            side = map_plot.height
            self.assertEqual((255, 0, 0), sheet.at(map_plot.left + side // 2, map_plot.top + side // 2))
            self.assertEqual((0, 0, 255), sheet.at(map_plot.left + side + side // 2, map_plot.top + side // 2))
            self.assertEqual((0, 0, 0), sheet.at(map_plot.left + 2 * side + side // 2, map_plot.top + side // 2))

    def test_a_label_stands_on_the_side_of_its_line_the_layout_gives(self):
        layout = {"width": 800, "panel_height": 400, "font": 16, "panels": [{
            "kind": "line", "x": axes(), "y": axes(),
            "series": [
                {"label": "above it", "color": "#ff0000", "points": [[0.0, 7], [10.0, 7]], "label_at": 2},
                {"label": "below it", "color": "#0000ff", "points": [[0.0, 3], [10.0, 3]], "label_at": 2, "label_side": "below"},
            ]}]}
        with Made() as made:
            lines, sheet = draw(made, layout)
            plot = Plot(lines, 1)
            ink = lambda y0, y1: sheet.dark(plot.x(2), y0, plot.x(5), y1)

            self.assertGreater(ink(plot.y(7) - 30, plot.y(7) - 4), 30, "the label above its line is not drawn above it")
            self.assertGreater(ink(plot.y(3) + 4, plot.y(3) + 30), 30, "the label below its line is not drawn below it")
            self.assertLess(ink(plot.y(3) - 30, plot.y(3) - 4), 5, "the label below its line is drawn above it")

    def test_a_label_over_a_rising_line_stands_clear_of_it_across_its_width(self):
        layout = {"width": 800, "panel_height": 400, "font": 16, "panels": [{
            "kind": "line", "x": axes(), "y": axes(),
            "series": [{"label": "a label of some width", "color": "#ff0000", "points": [[0.0, 0], [10.0, 10]], "label_at": 2}]}]}
        with Made() as made:
            lines, sheet = draw(made, layout)
            plot = Plot(lines, 1)

            # The label's ink below the line, in the plot from where it starts to the plot's end.
            under = over = 0
            for x in range(plot.x(2), plot.left + plot.width):
                for y in range(plot.top, plot.top + plot.height):
                    if sheet.at(x, y) != (24, 24, 24):
                        continue
                    if y > plot.y(10.0 * (x - plot.left) / plot.width) + 2:
                        under += 1
                    else:
                        over += 1

            self.assertGreater(over, 30, "the label is not drawn")
            self.assertEqual(0, under)

    def test_an_axis_given_its_range_draws_no_tick_outside_it(self):
        # A title and a subtitle of a letter set the plot low enough for the stray tick to fall on
        # the sheet, and stand clear of the margin read.
        layout = {"width": 800, "panel_height": 400, "font": 16, "panels": [{
            "kind": "line", "title": "T", "subtitle": "s", "x": {"min": 0, "max": 10}, "y": {"min": 20, "max": 90},
            "series": [{"color": "#ff0000", "points": [[0.0, 30], [10.0, 80]]}]}]}
        with Made() as made:
            lines, sheet = draw(made, layout)
            plot = Plot(lines, 1)

            # The round ticks of 20 to 90 run on to 100, which lies above the plot, where this
            # layout draws nothing else.
            self.assertEqual(0, sheet.dark(plot.left - 40, plot.top - 60, plot.left - 4, plot.top - 8))
            self.assertGreater(sheet.dark(plot.left - 40, plot.y(80) - 10, plot.left - 4, plot.y(80) + 10), 10, "the tick at 80 is not drawn")

    def test_a_panel_of_another_kind_is_refused_by_name(self):
        with Made() as made:
            with open(made / "layout.json", "w", encoding="utf-8") as f:
                f.write('{"panels": [{"kind": "pie"}]}')

            code, printed = attempt("chart.py", made / "layout.json", made / "sheet.png")

            self.assertEqual(1, code)
            self.assertIn("Panel 1 is of the kind pie, where line, bars, table and map are drawn.", printed)
