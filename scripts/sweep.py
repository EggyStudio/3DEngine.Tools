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

# What is printed is UTF-8 whatever the system gives a pipe, cp1252 on Windows, so a place or a
# caption holding a dash or a mark reads back whole.
sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")

# The rules are commitcheck.py's own, imported from beside this script, which isolated mode leaves
# off the path, and with no compiled copy written beside it, so the sweep leaves the module's
# folder as it found it, inside an engine's submodule as well.
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from commitcheck import PICTURES, TEXT, line_faults, picture_faults  # noqa: E402

SHOWN = 10


def forbidden_names():
    """The names FORBIDDEN_NAMES gives as one pattern, or None where it is not set."""
    names = os.environ.get("FORBIDDEN_NAMES")
    try:
        return re.compile(names, re.I) if names else None
    except re.error as error:
        sys.exit(f"FORBIDDEN_NAMES is no regular expression, {error}.")


def read_tree(repo, forbidden):
    """Every fault of a checkout's tracked files, each its level, its rule, the file's path and the
    place with what was found, any name in either put out of sight, with the counts of the text
    files, the pictures and the tracked files read. scripts/daily.py reads trees by it as well."""
    listed = subprocess.run(["git", "-C", repo, "ls-files", "-z"], capture_output=True)
    if listed.returncode:
        sys.exit(f"git ls-files failed in {repo}. {listed.stderr.decode('utf-8', 'replace').strip()}")
    paths = [p for p in listed.stdout.decode("utf-8", "replace").split("\0") if p]
    hide = (lambda text: forbidden.sub("<a name>", text)) if forbidden else (lambda text: text)
    found = []

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
            found.append(("FAIL", "a forbidden name", hide(path), hide(path)))
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
                    found.append(("FAIL", "a forbidden name", hide(path), hide(f"{path}:{number}")))
        in_xml = False
        for number, line in enumerate(text.split("\n"), 1):
            faults, in_xml = line_faults(path, line, in_xml)
            for level, rule, said in faults:
                found.append((level, rule, hide(path), hide(f"{path}:{number} {said}")))

    # A standing picture its folder's captions.md gives no line is warned of, which the commit's
    # check refuses for a picture added, so those that came before it are counted.
    captioned = {}
    for path in paths:
        if PICTURES.match(path) and os.path.dirname(path) not in captioned:
            folder = os.path.dirname(path)
            try:
                with open(os.path.join(repo, folder, "captions.md"), encoding="utf-8", errors="replace") as f:
                    captioned[folder] = {line.split(" | ", 1)[0].strip() for line in f if " | " in line}
            except OSError:
                captioned[folder] = set()
        if PICTURES.match(path) and os.path.basename(path) not in captioned[os.path.dirname(path)]:
            folder = os.path.dirname(path)
            found.append(("WARN", "a picture its folder's captions.md gives no line", hide(path), hide(f"{path} has no line in {folder}/captions.md")))

    for picture in pictures:
        path, faults = picture.result()
        for level, rule, said in faults:
            found.append((level, rule, hide(path), hide(f"{path} {said}")))
    pool.shutdown()
    return found, texts, len(pictures), len(paths)


def main():
    if len(sys.argv) != 2:
        sys.exit("Usage is sweep.py <repo>.")
    started = time.monotonic()
    forbidden = forbidden_names()
    found, texts, pictures, tracked = read_tree(sys.argv[1], forbidden)

    # Each rule's level, count and first places, in the order the rules are first broken.
    rules = {}
    for level, rule, _, place in found:
        entry = rules.setdefault(rule, [level, 0, []])
        entry[1] += 1
        if len(entry[2]) < SHOWN:
            entry[2].append(place)
    for rule, (level, count, places) in rules.items():
        print(f"{level} {rule}, {count}")
        for place in places:
            print(f"  {place[:150]}")
    if not forbidden:
        print("WARN FORBIDDEN_NAMES is not set, so the names were not read for")
    print(f"swept {texts} text files and {pictures} pictures of {tracked} tracked in {time.monotonic() - started:.1f} s")
    sys.exit(1 if any(level == "FAIL" for level, _, _ in rules.values()) else 0)


def read_picture(full, path):
    try:
        from PIL import Image
    except ImportError:
        return path, [("WARN", "a picture not read, since Pillow is missing", "")]
    with Image.open(full) as image:
        return path, picture_faults(image.convert("RGB"), path)


# Run as a script it sweeps; imported, as scripts/daily.py imports it, it gives read_tree.
if __name__ == "__main__":
    main()
