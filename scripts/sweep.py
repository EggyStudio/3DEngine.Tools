# Reads a checkout's tracked files as they stand by the rules scripts/commitcheck.py reads a
# commit's added lines with, a comment line's width, the comment patterns, the middle dot, the
# dashes and the spelling, the names FORBIDDEN_NAMES gives, and every picture under the readings or
# devlog folders by the pictures' rule. It writes nothing.
#
#   python3 -I scripts/sweep.py <repo>
#
# It prints a count a rule with the first ten places of each, then the files and pictures read and
# the time taken, and exits with 1 where any rule that fails a commit is broken. The names come from
# the environment alone, as commitcheck.py has them, and are never printed.
import os, re, subprocess, sys, time
from concurrent.futures import ThreadPoolExecutor

# The rules are commitcheck.py's own, imported from beside this script, which isolated mode leaves
# off the path, and with no compiled copy written beside it, so the sweep leaves the module's
# folder as it found it, inside an engine's submodule as well.
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from commitcheck import PICTURES, TEXT, line_faults, picture_faults  # noqa: E402

SHOWN = 10


def main():
    if len(sys.argv) != 2:
        sys.exit("Usage is sweep.py <repo>.")
    repo = sys.argv[1]
    started = time.monotonic()
    names = os.environ.get("FORBIDDEN_NAMES")
    try:
        forbidden = re.compile(names, re.I) if names else None
    except re.error as error:
        sys.exit(f"FORBIDDEN_NAMES is no regular expression, {error}.")
    listed = subprocess.run(["git", "-C", repo, "ls-files", "-z"], capture_output=True)
    if listed.returncode:
        sys.exit(f"git ls-files failed in {repo}. {listed.stderr.decode('utf-8', 'replace').strip()}")
    paths = [p for p in listed.stdout.decode("utf-8", "replace").split("\0") if p]

    # Each rule's level, count and first places, in the order the rules are first broken.
    rules = {}

    # A place is printed with any name in it put out of sight, a file's path among them.
    def found(level, rule, place):
        entry = rules.setdefault(rule, [level, 0, []])
        entry[1] += 1
        if len(entry[2]) < SHOWN:
            entry[2].append(forbidden.sub("<a name>", place) if forbidden else place)

    # Decoding the pictures takes most of a sweep's time where a tree keeps many, and Pillow's
    # decoders let other threads run meanwhile, so each is decoded on a thread of its own while the
    # text files are read.
    pool = ThreadPoolExecutor()
    texts, pictures = 0, []
    for path in paths:
        full = os.path.join(repo, path)
        if not os.path.isfile(full):
            continue
        if forbidden and forbidden.search(path):
            found("FAIL", "a forbidden name", path)
        if PICTURES.match(path):
            pictures.append(pool.submit(read_picture, full, path))
            continue
        if not TEXT.search(path):
            continue
        texts += 1
        with open(full, encoding="utf-8", errors="replace") as f:
            text = f.read()
        # The whole file is searched once for the names, and a line is read only where it is found.
        if forbidden and forbidden.search(text):
            for number, line in enumerate(text.split("\n"), 1):
                if forbidden.search(line):
                    found("FAIL", "a forbidden name", f"{path}:{number}")
        in_xml = False
        for number, line in enumerate(text.split("\n"), 1):
            faults, in_xml = line_faults(path, line, in_xml)
            for level, rule, said in faults:
                found(level, rule, f"{path}:{number} {said}")

    for picture in pictures:
        path, faults = picture.result()
        for level, rule, said in faults:
            found(level, rule, f"{path} {said}")
    pool.shutdown()

    for rule, (level, count, places) in rules.items():
        print(f"{level} {rule}, {count}")
        for place in places:
            print(f"  {place[:150]}")
    if not names:
        print("WARN FORBIDDEN_NAMES is not set, so the names were not read for")
    print(f"swept {texts} text files and {len(pictures)} pictures of {len(paths)} tracked in {time.monotonic() - started:.1f} s")
    sys.exit(1 if any(level == "FAIL" for level, _, _ in rules.values()) else 0)


def read_picture(full, path):
    try:
        from PIL import Image
    except ImportError:
        return path, [("WARN", "a picture not read, since Pillow is missing", "")]
    with Image.open(full) as image:
        return path, picture_faults(image.convert("RGB"), path)


main()
