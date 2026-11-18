# Composes a sheet of pictures on a white ground from a layout in JSON, rows of panels with a
# heading a column and a title a row in dark text, the form N 4.9 of NORM.md gives a picture that
# reads something. A row's title stands first, on a line of its own in a larger type, and the
# panels' labels under it, the first row's taking the columns' headings where a panel has none.
#
#   python3 -I scripts/sheet.py <layout.json> <sheet.png>
#
# A panel is one of three. A capture is a picture Pillow opens or a PFM of linear light, shown in
# sRGB at its row's gain. A signed difference is a capture less a reference in luminance, red where
# the capture is brighter and blue where it is darker, full at a share of the reference's mean, on
# black, the one dark panel of a sheet, so the least difference stands out from its ground. A
# difference is the largest channel's difference of two pictures as they are shown, times a factor,
# dark on white. A box is drawn on a capture only with the mean luminance it measures there written
# inside it, and the reading is printed as well, so every number on a sheet is one the script read.
# The layout's keys are in README.md.
#
# Each title's, labels' and panel's place on the sheet is printed, so a test or a note finds a panel
# without knowing the spacing. The sheet is written in the format its name's extension gives.
import json, math, os, sys
from array import array
from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageOps

GAP = 10
# Rec. 709's weights, those of sRGB's primaries, which the engines' captures are in.
LUMA = (0.2126, 0.7152, 0.0722)


def decode(v):
    return v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4


def encode(v):
    v = min(1.0, max(0.0, v))
    return round(255 * (12.92 * v if v <= 0.0031308 else 1.055 * v ** (1 / 2.4) - 0.055))


def floats(image):
    values = array("f")
    values.frombytes(image.tobytes())
    return values


def channel(size, values):
    return Image.frombytes("F", size, values.tobytes())


class Picture:
    """Three channels of linear light, each an image of 32-bit floats, rows from the top."""

    def __init__(self, channels):
        self.channels = channels
        self.size = channels[0].size

    def crop(self, box):
        return Picture([c.crop(box) for c in self.channels])

    def resized(self, size):
        # Resampled in linear light, so a panel made smaller keeps the light it holds.
        return self if size == self.size else Picture([c.resize(size, Image.LANCZOS) for c in self.channels])

    def luminance(self):
        r, g, b = (floats(c) for c in self.channels)
        return array("f", (LUMA[0] * r[i] + LUMA[1] * g[i] + LUMA[2] * b[i] for i in range(len(r))))

    def mean(self, box=None):
        values = (self.crop(box) if box else self).luminance()
        return math.fsum(values) / len(values)

    def shown(self, gain):
        table = {}
        bands = []
        for c in self.channels:
            band = bytes(table.setdefault(v, encode(v * gain)) for v in floats(c))
            bands.append(Image.frombytes("L", self.size, band))
        return Image.merge("RGB", bands)


def read(path):
    if not os.path.exists(path):
        sys.exit(f"{path} does not exist.")
    if path.lower().endswith(".pfm"):
        return read_pfm(path)
    image = Image.open(path).convert("RGB")
    table = [decode(i / 255) for i in range(256)]
    return Picture([channel(image.size, array("f", (table[v] for v in band.getdata()))) for band in image.split()])


def read_pfm(path):
    with open(path, "rb") as f:
        kind = f.readline().strip()
        width, height = (int(v) for v in f.readline().split())
        scale = float(f.readline())
        count = 3 if kind == b"PF" else 1
        data = array("f")
        data.frombytes(f.read(width * height * count * 4))
    # A negative scale marks little-endian floats, a positive one big-endian.
    if (scale < 0) != (sys.byteorder == "little"):
        data.byteswap()
    # PFM's rows run from the bottom up, and a grayscale PFM's one channel is shown as gray.
    planes = [channel((width, height), data[c::count]).transpose(Image.FLIP_TOP_BOTTOM) for c in range(count)]
    return Picture(planes * 3 if count == 1 else planes)


def main():
    layout_path, out = sys.argv[1], sys.argv[2]
    layout = json.load(open(layout_path, encoding="utf-8"))
    base = os.path.dirname(os.path.abspath(layout_path))
    font_size = layout.get("font", 16)
    font = ImageFont.load_default(size=font_size)
    line = font_size + 10
    title_size = round(font_size * 1.3)
    title_font = ImageFont.load_default(size=title_size)
    title_line = title_size + 12
    pictures = {}

    def picture(name):
        path = os.path.join(base, name)
        if path not in pictures:
            pictures[path] = read(path)
        return pictures[path]

    def gain_of(value):
        if isinstance(value, dict):
            return value["to"] / picture(value["mean"]).mean()
        return float(value)

    # Every panel is made before any is placed, so each column is as wide as its widest panel.
    rows = []
    for r, row in enumerate(layout["rows"], 1):
        panels = []
        for c, panel in enumerate(row["panels"], 1):
            options = {**layout, **row, **panel}
            crop = tuple(options["crop"]) if "crop" in options else None
            gain = gain_of(options.get("gain", 1))

            def prepared(name):
                captured = picture(name)
                return captured.crop(crop) if crop else captured

            if "image" in panel:
                captured = prepared(panel["image"])
                size = tuple(options.get("size", captured.size))
                image = captured.resized(size).shown(gain)
            elif "signed" in panel:
                frame, reference = (prepared(name) for name in panel["signed"])
                full = options.get("share", 0.5) * reference.mean()
                size = tuple(options.get("size", frame.size))
                f, ref = frame.resized(size).luminance(), reference.resized(size).luminance()
                pixels = []
                for i in range(len(f)):
                    t = max(-1.0, min(1.0, (f[i] - ref[i]) / full))
                    k = round(255 * abs(t))
                    pixels.append((k, 0, 0) if t > 0 else (0, 0, k))
                image = Image.new("RGB", size)
                image.putdata(pixels)
            elif "difference" in panel:
                first, second = (prepared(name) for name in panel["difference"])
                size = tuple(options.get("size", first.size))
                apart = ImageChops.difference(first.resized(size).shown(gain), second.resized(size).shown(gain))
                largest = ImageChops.lighter(ImageChops.lighter(*apart.split()[:2]), apart.split()[2])
                times = options.get("times", 4)
                image = ImageOps.invert(largest.point(lambda v: min(255, round(v * times)))).convert("RGB")
            else:
                sys.exit(f"Row {r}'s panel {c} names no image, signed or difference.")

            boxes = panel.get("boxes", [])
            if boxes and "image" not in panel:
                sys.exit(f"Row {r}'s panel {c} has a box, which only a capture's panel reads.")
            readings = [(tuple(box), picture(panel["image"]).mean(tuple(box))) for box in boxes]
            panels.append({"image": image, "label": panel.get("label"), "readings": readings, "crop": crop,
                           "source": picture(panel["image"]).size if "image" in panel else None})
        rows.append((row.get("title"), panels))

    columns = layout.get("columns", [])
    widths = [0] * max(len(columns), max(len(panels) for _, panels in rows))
    for _, panels in rows:
        for c, panel in enumerate(panels):
            widths[c] = max(widths[c], panel["image"].size[0])
    lefts = [GAP + sum(w + GAP for w in widths[:c]) for c in range(len(widths))]
    # The first row's labels take the columns' headings where its panels have none of their own.
    for c, heading in enumerate(columns[:len(rows[0][1])]):
        rows[0][1][c]["label"] = rows[0][1][c]["label"] or heading
    height = GAP
    for title, panels in rows:
        height += (title_line if title else 0) + (line if any(p["label"] for p in panels) else 0)
        height += max(p["image"].size[1] for p in panels) + GAP

    sheet = Image.new("RGB", (GAP + sum(w + GAP for w in widths), height), "white")
    draw = ImageDraw.Draw(sheet)
    y = GAP
    printed = [f"sheet {out} {sheet.size[0]}x{sheet.size[1]}"]
    for r, (title, panels) in enumerate(rows, 1):
        if title:
            draw.text((GAP, y), title, fill="black", font=title_font)
            printed.append(f"row {r} title at {GAP},{y} height {title_line}")
            y += title_line
        if any(p["label"] for p in panels):
            for c, panel in enumerate(panels):
                if panel["label"]:
                    draw.text((lefts[c], y), panel["label"], fill="black", font=font)
            printed.append(f"row {r} labels at {GAP},{y} height {line}")
            y += line
        for c, panel in enumerate(panels, 1):
            image, x = panel["image"], lefts[c - 1]
            sheet.paste(image, (x, y))
            printed.append(f"row {r} panel {c} at {x},{y} size {image.size[0]}x{image.size[1]}")
            # A box is given in the capture's own pixels, so it is moved by the crop and scaled
            # with the panel, and its reading is written at its corner on white.
            x0, y0 = panel["crop"][:2] if panel["crop"] else (0, 0)
            if panel["source"]:
                across = (panel["crop"][2] - x0) if panel["crop"] else panel["source"][0]
                down = (panel["crop"][3] - y0) if panel["crop"] else panel["source"][1]
            for b, (box, reading) in enumerate(panel["readings"], 1):
                sx, sy = image.size[0] / across, image.size[1] / down
                left, top = x + (box[0] - x0) * sx, y + (box[1] - y0) * sy
                right, bottom = x + (box[2] - x0) * sx - 1, y + (box[3] - y0) * sy - 1
                draw.rectangle((left, top, right, bottom), outline="black", width=1)
                draw.rectangle((left + 1, top + 1, right - 1, bottom - 1), outline="white", width=1)
                text = f"{reading:.4g}"
                corner = draw.textbbox((left + 2, top + 2), text, font=font)
                draw.rectangle((corner[0] - 1, corner[1] - 1, corner[2] + 1, corner[3] + 1), fill="white")
                draw.text((left + 2, top + 2), text, fill="black", font=font)
                printed.append(f"row {r} panel {c} box {b} at {','.join(str(v) for v in box)} reads {reading:.4g}")
        y += max(p["image"].size[1] for p in panels) + GAP

    sheet.save(out)
    print("\n".join(printed))


main()
