# What the tests share: the folder, a script run as the page runs it, a made folder, made pictures
# and a drawn sheet's pixels read.
import importlib.util, os, re, shutil, struct, subprocess, sys, tempfile, unittest

FOLDER = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PILLOW = importlib.util.find_spec("PIL") is not None
needs_pillow = unittest.skipUnless(PILLOW, "Pillow is missing here, which the scripts draw with.")


def attempt(script, *arguments):
    """A script of the folder run with the arguments, its exit code and what it printed."""
    done = subprocess.run([sys.executable, "-I", os.path.join(FOLDER, script), *arguments],
                          capture_output=True, text=True, encoding="utf-8")
    return done.returncode, done.stdout + done.stderr


def run(script, *arguments):
    """A script that must succeed, its printed lines."""
    code, printed = attempt(script, *arguments)
    if code != 0:
        raise AssertionError(f"{script} exited with {code}. {printed}")
    if "Traceback (most recent call last)" in printed:
        raise AssertionError(f"{script} wrote a traceback. {printed}")
    return printed.replace("\r\n", "\n").split("\n")


class Made:
    """A temporary folder, deleted with its files when the test ends."""

    def __enter__(self):
        self.path = tempfile.mkdtemp(prefix="sheets-")
        return self

    def __exit__(self, *_):
        shutil.rmtree(self.path, ignore_errors=True)

    def __truediv__(self, name):
        return os.path.join(self.path, name)


def write_pfm(path, width, height, light):
    # PFM's rows run from the bottom up, and a negative scale marks its floats little-endian.
    with open(path, "wb") as f:
        f.write(f"PF\n{width} {height}\n-1.0\n".encode("ascii"))
        for y in range(height - 1, -1, -1):
            for x in range(width):
                f.write(struct.pack("<3f", *[light(x, y)] * 3))


def write_ppm(path, width, height, gray):
    with open(path, "wb") as f:
        f.write(f"P6\n{width} {height}\n255\n".encode("ascii"))
        f.write(bytes(v for y in range(height) for x in range(width) for v in [gray(x, y)] * 3))


class Picture:
    """A drawn sheet's pixels, read with Pillow."""

    def __init__(self, path):
        from PIL import Image
        self.image = Image.open(path).convert("RGB")
        self.width, self.height = self.image.size
        self.pixels = self.image.load()

    def at(self, x, y):
        return self.pixels[x, y]

    def dark(self, x0, y0, x1, y1):
        """The pixels of a box whose channels sum under 300, the ink of text and lines."""
        return sum(1 for y in range(y0, y1) for x in range(x0, x1) if sum(self.pixels[x, y]) < 300)


def single(lines, start):
    found = [line for line in lines if line.startswith(start)]
    if len(found) != 1:
        raise AssertionError(f"{len(found)} lines begin {start!r}")
    return found[0]


PLOT = re.compile(r"plot at (?P<x>\d+),(?P<y>\d+) size (?P<w>\d+)x(?P<h>\d+)( x (?P<xl>[\d,.]+)\.\.(?P<xh>[\d,.]+)( log)?"
                  r"( y (?P<yl>[\d,.]+)\.\.(?P<yh>[\d,.]+))?)?")


class Plot:
    """A chart's plot as the script prints it, its place, its size and its axes' ranges, mapping a
    value to its pixel."""

    def __init__(self, lines, panel):
        line = single(lines, f"panel {panel} plot at ")
        found = PLOT.search(line)
        number = lambda g: float(found.group(g).replace(",", "")) if found.group(g) else 0.0
        self.left, self.top, self.width, self.height = (int(number(g)) for g in "xywh")
        self.xl, self.xh, self.yl, self.yh = (number(g) for g in ("xl", "xh", "yl", "yh"))
        self.log = line.endswith(" log")

    def x(self, value):
        return round(self.left + (value - self.xl) / (self.xh - self.xl) * self.width)

    def y(self, value):
        import math
        share = ((math.log10(value) - math.log10(self.yl)) / (math.log10(self.yh) - math.log10(self.yl))
                 if self.log else (value - self.yl) / (self.yh - self.yl))
        return round(self.top + self.height - share * self.height)
