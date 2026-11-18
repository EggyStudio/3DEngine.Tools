# Draws a sheet of data charts on a white ground from a layout in JSON: a title and a subtitle,
# panels of line charts, horizontal bars by group, tables and signed maps in a grid, and a line of
# notes, written in the format the output's name gives, WebP or PNG among them. It needs Pillow and
# nothing else.
#
#   python3 -I chart.py <layout.json> <sheet.webp>
#
# A line chart's axes are linear or logarithmic, its series solid or dashed, each labeled above or
# below the line where the layout says, its points marked where asked, and a band shaded between two
# values with a label. Bars stand by group, a bar a series, the series in a legend. A table holds a
# header and rows, a row bold where asked. A map shows a grid of signed values, red above zero and
# blue below, full at the scale given and black at zero, so the least value stands out from its
# ground. Each panel's place on the sheet is printed, and each chart's plot with its axes' ranges,
# so a test or a note finds a value's pixel without knowing the spacing. The layout's keys are in
# the README.
import json, math, os, sys
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")

GAP = 24
INK, GRID, MUTED, BAND = (24, 24, 24), (226, 226, 226), (96, 96, 96), (238, 236, 232)
# A palette whose colors stay apart to a reader of either common kind of color blindness.
PALETTE = ["#1f5fbf", "#e8590c", "#2f9e44", "#7048e8", "#c2255c", "#495057", "#0c8599", "#b08800"]


def color(value, index):
    if isinstance(value, str) and value.startswith("#") and len(value) == 7:
        return tuple(int(value[i:i + 2], 16) for i in (1, 3, 5))
    return color(PALETTE[index % len(PALETTE)], 0)


def nice(low, high, count=5):
    """Ticks at round numbers across a range, about count of them."""
    if high <= low:
        high = low + 1
    raw = (high - low) / count
    power = 10 ** math.floor(math.log10(raw))
    step = min((s * power for s in (1, 2, 2.5, 5, 10) if s * power >= raw), default=10 * power)
    # The ticks reach the first round number at or past each end, so the axis holds every value.
    first = math.floor(low / step + 1e-9) * step
    last = math.ceil(high / step - 1e-9) * step
    return [round(first + i * step, 10) for i in range(int(round((last - first) / step)) + 1)]


def figures(value, count=3):
    """A value to three significant figures, as a bar's label gives it, a whole count whole."""
    # JSON keeps 35 apart from 35.0, so a count written whole reads whole and a measure keeps its
    # figures.
    if isinstance(value, int) and not isinstance(value, bool):
        return f"{value:,}"
    if value == 0:
        return "0"
    places = max(0, count - 1 - math.floor(math.log10(abs(value))))
    return f"{value:,.{places}f}"


def text_of(value):
    if isinstance(value, float) and value.is_integer():
        value = int(value)
    if isinstance(value, int):
        return f"{value:,}"
    return f"{value:,.4g}" if isinstance(value, float) else str(value)


class Axis:
    """An axis's range and its map to pixels, linear or logarithmic."""

    def __init__(self, spec, values, start, length, flip):
        self.log = bool(spec.get("log"))
        self.label = spec.get("label", "")
        found = [v for v in values if not self.log or v > 0]
        low = spec.get("min", min(found) if found else 0)
        high = spec.get("max", max(found) if found else 1)
        if self.log:
            low, high = 10 ** math.floor(math.log10(low)), 10 ** math.ceil(math.log10(high))
            self.ticks = spec.get("ticks") or [10 ** p for p in range(round(math.log10(low)), round(math.log10(high)) + 1)]
        else:
            self.ticks = spec.get("ticks") or nice(low, high)
            if "min" not in spec: low = min(low, self.ticks[0])
            if "max" not in spec: high = max(high, self.ticks[-1])
        # A range the layout gives keeps a round tick past it off the axis, where it would stand
        # over the panel's title.
        span = (high - low) * 1e-9
        self.ticks = [t for t in self.ticks if low - span <= t <= high + span]
        self.low, self.high, self.start, self.length, self.flip = low, high, start, length, flip

    def value(self, pixel):
        """The value at a pixel, the map at gives turned the other way."""
        share = (pixel - self.start) / self.length
        share = 1 - share if self.flip else share
        if self.log:
            return 10 ** (math.log10(self.low) + share * (math.log10(self.high) - math.log10(self.low)))
        return self.low + share * (self.high - self.low)

    def at(self, value):
        if self.log:
            share = (math.log10(max(value, 1e-300)) - math.log10(self.low)) / (math.log10(self.high) - math.log10(self.low))
        else:
            share = (value - self.low) / ((self.high - self.low) or 1)
        return self.start + (self.length - share * self.length if self.flip else share * self.length)


class Sheet:
    def __init__(self, layout):
        self.layout = layout
        size = layout.get("font", 16)
        self.font = ImageFont.load_default(size=size)
        self.small = ImageFont.load_default(size=round(size * 0.85))
        self.title_font = ImageFont.load_default(size=round(size * 1.5))
        self.heading = ImageFont.load_default(size=round(size * 1.15))
        self.line = size + 8
        self.printed = []

    def width_of(self, draw, text, font):
        box = draw.textbbox((0, 0), text, font=font)
        return box[2] - box[0]

    def draw(self, out):
        layout = self.layout
        columns = layout.get("columns", 1)
        width = layout.get("width", 1600)
        cell = (width - GAP * (columns + 1)) // columns
        height = layout.get("panel_height", 480)
        panels = layout["panels"]
        rows = math.ceil(len(panels) / columns)
        top = GAP + (round(self.title_font.size * 1.5) if layout.get("title") else 0) + (self.line if layout.get("subtitle") else 0)
        top += self.line + GAP // 2 if layout.get("legend") else 0
        total = top + rows * (height + GAP) + (self.line + GAP if layout.get("notes") else 0)
        sheet = Image.new("RGB", (width, total), "white")
        draw = ImageDraw.Draw(sheet)

        y = GAP
        if layout.get("title"):
            draw.text((GAP, y), layout["title"], fill=INK, font=self.title_font)
            y += round(self.title_font.size * 1.5)
        if layout.get("subtitle"):
            draw.text((GAP, y), layout["subtitle"], fill=MUTED, font=self.font)
            y += self.line
        if layout.get("legend"):
            x = GAP
            for i, entry in enumerate(layout["legend"]):
                draw.rounded_rectangle((x, y + 4, x + 14, y + 18), radius=3, fill=color(entry.get("color"), i))
                draw.text((x + 20, y + 2), entry["label"], fill=INK, font=self.font)
                x += 20 + self.width_of(draw, entry["label"], self.font) + 3 * GAP

        self.printed.append(f"sheet {out} {width}x{total}")
        for n, panel in enumerate(panels, 1):
            px, py = GAP + ((n - 1) % columns) * (cell + GAP), top + ((n - 1) // columns) * (height + GAP)
            self.printed.append(f"panel {n} {panel['kind']} at {px},{py} size {cell}x{height}")
            inner = py
            if panel.get("title"):
                draw.text((px, inner), panel["title"], fill=INK, font=self.heading)
                inner += round(self.heading.size * 1.5)
            if panel.get("subtitle"):
                draw.text((px, inner), panel["subtitle"], fill=MUTED, font=self.small)
                inner += self.line
            box = (px, inner + 8, px + cell, py + height)
            kind = {"line": self.line_chart, "bars": self.bars, "table": self.table, "map": self.map}.get(panel["kind"])
            if kind is None:
                sys.exit(f"Panel {n} is of the kind {panel['kind']}, where line, bars, table and map are drawn.")
            kind(draw, sheet, n, panel, box)

        if layout.get("notes"):
            draw.text((GAP, total - GAP - self.line + 4), layout["notes"], fill=MUTED, font=self.small)
        sheet.save(out, **({"quality": 90, "method": 6} if out.lower().endswith(".webp") else {}))
        print("\n".join(self.printed))

    # -- A line chart

    def line_chart(self, draw, sheet, n, panel, box):
        series = panel.get("series", [])
        xs = [p[0] for s in series for p in s["points"]]
        ys = [p[1] for s in series for p in s["points"]]
        for band in panel.get("bands", []):
            ys += [band["from"], band["to"]]
        left, right, top, bottom = box[0] + 70, box[2] - 16, box[1] + 8, box[3] - 56
        x = Axis(panel.get("x", {}), xs, left, right - left, flip=False)
        y = Axis(panel.get("y", {}), ys, top, bottom - top, flip=True)
        self.printed.append(f"panel {n} plot at {left},{top} size {right - left}x{bottom - top} "
                            f"x {text_of(x.low)}..{text_of(x.high)}{' log' if x.log else ''} y {text_of(y.low)}..{text_of(y.high)}{' log' if y.log else ''}")

        for band in panel.get("bands", []):
            x0, x1 = x.at(band.get("x_from", x.low)), x.at(band.get("x_to", x.high))
            draw.rectangle((x0, y.at(band["to"]), x1, y.at(band["from"])), fill=BAND)
            if band.get("label"):
                label = band["label"]
                draw.text((x1 - self.width_of(draw, label, self.small) - 8, y.at(band["to"]) + 6), label, fill=MUTED, font=self.small)
        for tick in y.ticks:
            at = y.at(tick)
            draw.line((left, at, right, at), fill=GRID, width=1)
            label = text_of(tick)
            draw.text((left - 10 - self.width_of(draw, label, self.small), at - self.small.size // 2 - 2), label, fill=MUTED, font=self.small)
        for tick in x.ticks:
            at = x.at(tick)
            label = text_of(tick)
            draw.text((at - self.width_of(draw, label, self.small) / 2, bottom + 8), label, fill=MUTED, font=self.small)
        if x.label:
            draw.text(((left + right) / 2 - self.width_of(draw, x.label, self.font) / 2, bottom + 30), x.label, fill=INK, font=self.font)
        if y.label:
            label = Image.new("RGBA", (self.width_of(draw, y.label, self.font) + 4, self.font.size + 8), (255, 255, 255, 0))
            ImageDraw.Draw(label).text((0, 0), y.label, fill=INK, font=self.font)
            turned = label.rotate(90, expand=True)
            sheet.paste(turned, (box[0], int((top + bottom) / 2 - turned.size[1] / 2)), turned)

        for i, s in enumerate(series):
            ink = color(s.get("color"), i)
            points = [(x.at(px), y.at(py)) for px, py in s["points"] if (not x.log or px > 0) and (not y.log or py > 0)]
            if s.get("dash"):
                self.dashed(draw, points, ink)
            else:
                draw.line(points, fill=ink, width=3, joint="curve")
            if s.get("marks"):
                for mx, my in points:
                    draw.ellipse((mx - 5, my - 5, mx + 5, my + 5), fill=ink, outline="white")
            for mark in s.get("marked", []):
                mx, my = x.at(mark[0]), y.at(mark[1])
                draw.ellipse((mx - 6, my - 6, mx + 6, my + 6), fill=ink, outline="white", width=2)
            if s.get("label"):
                lx = s.get("label_at", x.low + 0.7 * (x.high - x.low) if not x.log else math.sqrt(x.low * x.high))
                ly = self.value_at(s["points"], lx)
                if ly is not None:
                    # Above its line, or below it where another line runs just above, clear of
                    # the line across the label's whole width, where a line that rises or falls
                    # under the text would cross it.
                    below = s.get("label_side") == "below"
                    start, width = x.at(lx) + 4, self.width_of(draw, s["label"], self.small)
                    heights = [y.at(v) for v in (self.value_at(s["points"], x.value(px)) for px in range(int(start), int(start + width) + 1)) if v is not None]
                    edge = max(heights) if below else min(heights)
                    draw.text((start, edge + 6 if below else edge - self.small.size - 8), s["label"], fill=INK, font=self.small)

    @staticmethod
    def value_at(points, at):
        for (x0, y0), (x1, y1) in zip(points, points[1:]):
            if min(x0, x1) <= at <= max(x0, x1):
                return y0 if x1 == x0 else y0 + (y1 - y0) * (at - x0) / (x1 - x0)
        return None

    @staticmethod
    def dashed(draw, points, ink, on=10, off=7):
        left, drawing = on, True
        for (x0, y0), (x1, y1) in zip(points, points[1:]):
            length = math.hypot(x1 - x0, y1 - y0)
            done = 0.0
            while done < length:
                step = min(left, length - done)
                a, b = done / length, (done + step) / length
                if drawing:
                    draw.line((x0 + (x1 - x0) * a, y0 + (y1 - y0) * a, x0 + (x1 - x0) * b, y0 + (y1 - y0) * b), fill=ink, width=3)
                done += step
                left -= step
                if left <= 0:
                    drawing, left = not drawing, on if not drawing else off

    # -- Bars by group

    def bars(self, draw, sheet, n, panel, box):
        groups = panel["groups"]
        names = panel.get("series", [])
        count = max(len(g["values"]) for g in groups)
        values = [v for g in groups for v in g["values"] if v is not None]
        label_width = max(self.width_of(draw, g["label"], self.font) for g in groups) + 16
        left, right, top, bottom = box[0] + label_width, box[2] - 56, box[1] + 4, box[3] - 48
        # The series' labels stand as the panel's legend over its plot, where the layout gives no
        # legend of its own for the sheet.
        labeled = [(i, entry) for i, entry in enumerate(names) if entry.get("label")]
        if labeled and not self.layout.get("legend"):
            lx = left
            for i, entry in labeled:
                draw.rounded_rectangle((lx, top + 4, lx + 14, top + 18), radius=3, fill=color(entry.get("color"), i))
                draw.text((lx + 20, top + 2), entry["label"], fill=INK, font=self.font)
                self.printed.append(f"panel {n} legend {i + 1} at {lx},{top + 4}")
                lx += 20 + self.width_of(draw, entry["label"], self.font) + 3 * GAP
            top += self.line + GAP // 2
        x = Axis(panel.get("x", {}), values + [0], left, right - left, flip=False)
        self.printed.append(f"panel {n} plot at {left},{top} size {right - left}x{bottom - top} x {text_of(x.low)}..{text_of(x.high)}")
        for tick in x.ticks:
            at = x.at(tick)
            draw.line((at, top, at, bottom), fill=GRID, width=1)
            label = text_of(tick)
            draw.text((at - self.width_of(draw, label, self.small) / 2, bottom + 6), label, fill=MUTED, font=self.small)
        draw.line((x.at(max(0, x.low)), top, x.at(max(0, x.low)), bottom), fill=MUTED, width=1)
        if x.label:
            draw.text(((left + right) / 2 - self.width_of(draw, x.label, self.small) / 2, bottom + 26), x.label, fill=MUTED, font=self.small)

        slot = (bottom - top) / len(groups)
        bar = max(3, min(14, (slot - 18) / count))
        shown = panel.get("value_labels", "last")
        for g, group in enumerate(groups):
            gy = top + g * slot + (slot - bar * count) / 2
            draw.text((left - 10 - self.width_of(draw, group["label"], self.font), gy + bar * count / 2 - self.font.size / 2 - 2),
                      group["label"], fill=INK, font=self.font)
            for s, value in enumerate(group["values"]):
                if value is None:
                    continue
                ink = color(names[s].get("color") if s < len(names) else None, s)
                y0 = gy + s * bar
                x0, x1 = x.at(0), x.at(value)
                if abs(x1 - x0) >= 1:
                    draw.rounded_rectangle((min(x0, x1), y0 + 1, max(x0, x1), y0 + bar - 1), radius=min(3, bar / 3), fill=ink)
                place = f"panel {n} bar {g + 1} {s + 1} at {round(min(x0, x1))},{round(y0 + 1)} to {round(max(x0, x1))},{round(y0 + bar - 1)}"
                if shown == "all" or shown == "last" and s == len(group["values"]) - 1:
                    draw.text((x.at(value) + 6, y0 + bar / 2 - self.small.size / 2 - 2), figures(value), fill=MUTED, font=self.small)
                    place += f" labeled {figures(value)}"
                self.printed.append(place)

    # -- A table

    def table(self, draw, sheet, n, panel, box):
        columns, rows = panel["columns"], panel["rows"]
        bold = set(panel.get("bold", []))
        align = panel.get("align", ["left"] + ["right"] * (len(columns) - 1))
        every = [columns] + rows
        widths = [max(self.width_of(draw, text_of(r[c]), self.font) for r in every) + 24 for c in range(len(columns))]
        x0, y = box[0], box[1]
        height = self.line + 6
        for r, row in enumerate(every):
            heavy = r == 0 or (r - 1) in bold
            x = x0
            for c, cell in enumerate(row):
                text = text_of(cell)
                tx = x + (widths[c] - 24 - self.width_of(draw, text, self.font) if align[c] == "right" else 0)
                draw.text((tx, y + 3), text, fill=INK, font=self.font)
                if heavy:
                    # The default face has no bold of its own, so a heavy row is drawn twice a
                    # pixel apart.
                    draw.text((tx + 1, y + 3), text, fill=INK, font=self.font)
                x += widths[c]
            self.printed.append(f"panel {n} row {r} at {x0},{y} size {sum(widths)}x{height}{' bold' if heavy and r else ''}")
            y += height
            draw.line((x0, y - 2, x0 + sum(widths), y - 2), fill=INK if r == 0 else GRID, width=1)

    # -- A signed map

    def map(self, draw, sheet, n, panel, box):
        values = panel["values"]
        rows, columns = len(values), max(len(r) for r in values)
        scale = panel.get("scale") or max((abs(v) for r in values for v in r), default=1) or 1
        side = int(min((box[2] - box[0]) / columns, (box[3] - box[1] - self.line) / rows))
        left, top = box[0], box[1]
        self.printed.append(f"panel {n} plot at {left},{top} size {side * columns}x{side * rows} cells {columns}x{rows}")
        for r, row in enumerate(values):
            for c, v in enumerate(row):
                k = round(255 * min(1.0, abs(v) / scale))
                draw.rectangle((left + c * side, top + r * side, left + (c + 1) * side - 1, top + (r + 1) * side - 1),
                               fill=(k, 0, 0) if v > 0 else (0, 0, k))
        draw.text((left, top + rows * side + 6), panel.get("label", f"red above zero, blue below, full at {text_of(scale)}"),
                  fill=MUTED, font=self.small)


def main():
    if len(sys.argv) != 3:
        sys.exit("Usage is chart.py <layout.json> <sheet.webp>.")
    with open(sys.argv[1], encoding="utf-8") as f:
        layout = json.load(f)
    Sheet(layout).draw(sys.argv[2])


main()
