# Reads commits against the form COMMITS.md gives and the rules of NORM.md a pattern can read,
# before they are reported and pushed.
#
#   python3 -I scripts/commitcheck.py <repo> [<range or commit> ...] [--thumbs=<folder>]
#
# The range is origin/main..HEAD where none is given. A FAIL line stops a push and the exit code is
# 1 where any commit has one; a WARN line is read by the reviewer, who decides; a note says what to
# look at. The names no commit may carry come from the FORBIDDEN_NAMES environment variable alone,
# a regular expression, and stand in no file, so the list is never itself committed; where it is
# not set the check says it did not run. Each picture a commit adds under the readings is read for
# its white ground and saved smaller under --thumbs, a folder under the system's temporary one
# where none is given, to be looked at.
import io, os, re, subprocess, sys, tempfile

# Three left-to-right marks with a space between each, then the blank line that ends the subject.
LEAD = "‎ ‎ ‎\n\n"
AUTHOR = "EggyStudio"
MESSAGE_BAD = [
    ("a review's number", re.compile(r"\b(items?|verdicts?|decisions?|N)\s+\d+(\.\d+)?\b", re.I)),
    ("a tool or a session", re.compile(r"co-authored|claude|session", re.I)),
    ("a colon inside the sentence", re.compile(r"[\w)`'\"]: ")),
    ("a spaced hyphen", re.compile(r" - ")),
]
# A comment's mark, a preprocessor's directive excepted, or the opening of an MSBuild comment.
COMMENT = re.compile(r"^\s*(///?|//!|#(?!\s*(if|else|elif|endif|region|endregion|pragma|define|include|nullable)\b)|--|\*|/\*|<!--)")
COMMENT_BAD = [
    ("names the owner or a session", re.compile(r"\bowner\b|\bsession\b|\bclaude\b", re.I)),
    ("tells what the code was", re.compile(r"\b(used to|no longer|formerly|previously|earlier|was once|before this|had been|regress\w*)\b", re.I)),
    ("likens to another product", re.compile(r"\b(Unity|Unreal|Godot|Minecraft|HDRP|URP|Lumen|Nanite)\b")),
    ("names an asset or scene", re.compile(r"\b(Sponza|Bistro|San Miguel|san_giuseppe)\b", re.I)),
    ("carries a run's facts", re.compile(r"\b(runner|lavapipe run|CI run|run \d{6,}|20\d\d-\d\d-\d\d|\d+(\.\d+)? ?(ms|fps)\b|\d+ of \d+ (runs|starts))", re.I)),
    ("names a review's number", re.compile(r"\b(item|verdict|decision)\s+\d+\b", re.I)),
]
PROSE_BAD = [
    ("middle dot", re.compile("·")),
    ("em dash", re.compile("—")),
    ("en dash", re.compile("–")),
    ("British spelling", re.compile(r"\b(colou?r(?<=colour)\w*|grey\w*|centre[ds]?|honou?r(?<=honour)\w*|neighbou?r(?<=neighbour)\w*|behaviou?r(?<=behaviour)\w*|normalis\w+|initialis\w+|optimis\w+)\b", re.I)),
    ("\"word\" for four bytes", re.compile(r"\b(d?word)s?\b(?=.*\b(byte|bit|uint)\b)", re.I)),
]
SEEN = re.compile(r"\.(slang|wgsl|glsl|hlsl|frag|vert)$|(^|/)(Rendering|render|Shaders|shaders|Lighting|Ui)/")
PICTURES = re.compile(r"^\.github/(assets/readings|devlog)/.*\.webp$")
TEXT = re.compile(r"\.(cs|rs|slang|wgsl|py|sh|md|toml|yml|yaml|json|csproj|props|targets|slnx)$")
XML = re.compile(r"\.(csproj|props|targets|slnx|xml)$")
COLUMNS = 100


def git(repo, *arguments, raw=False):
    done = subprocess.run(["git", "-C", repo, *arguments], capture_output=True)
    if done.returncode:
        sys.exit(f"git {' '.join(arguments)} failed in {repo}. {done.stderr.decode('utf-8', 'replace').strip()}")
    return done.stdout if raw else done.stdout.decode("utf-8", "replace")


def picture(repo, rev, path, thumbs, fail, warn, note):
    try:
        from PIL import Image
    except ImportError:
        warn(f"{path}: Pillow is missing here, so the ground is not read")
        return
    image = Image.open(io.BytesIO(git(repo, "show", f"{rev}:{path}", raw=True))).convert("RGB")
    w, h = image.size
    corners = [image.getpixel(p) for p in ((2, 2), (w - 3, 2), (2, h - 3), (w - 3, h - 3))]
    if any(min(c) < 250 for c in corners):
        fail(f"{path}: the ground is not white, its corners {corners}")
    strip = image.crop((0, 0, w, max(1, h // 12))).convert("L")
    if strip.getextrema()[0] > 120:
        warn(f"{path}: no dark text in the top strip, so a title may be missing")
    if not re.match(r"^\d{4}-\d\d-\d\d-", os.path.basename(path)):
        warn(f"{path}: the name does not begin with a date")
    os.makedirs(thumbs, exist_ok=True)
    image.thumbnail((1800, 1800))
    out = os.path.join(thumbs, os.path.basename(path)[:-5] + ".png")
    image.save(out)
    note(f"{path}: {w}x{h}, look at {out}")


# The message is read from the commit object itself, where git log's format would add a line end of
# its own and leave the count of the message's lines to the format.
def message(repo, rev):
    commit = git(repo, "cat-file", "commit", rev)
    return commit.split("\n\n", 1)[1] if "\n\n" in commit else ""


def check(repo, rev, names, thumbs):
    fails, warns, notes = [], [], []
    fail, warn, note = fails.append, warns.append, notes.append
    text = message(repo, rev)
    if not text.startswith(LEAD):
        fail(f"the message does not begin with the three marks and a blank line, it begins {text[:13].encode('utf-8').hex()}")
    body = text[len(LEAD):] if text.startswith(LEAD) else text.split("\n\n", 1)[-1]
    lines = body.rstrip("\n").split("\n")
    if len(lines) != 1 or not body.endswith("\n"):
        fail(f"the message's description holds {len(lines)} lines, one sentence expected")
    sentence = lines[0].strip()
    if not sentence.endswith("."):
        fail("the sentence does not end with a full stop")
    if sentence and not sentence[0].isupper() and not sentence[0] in "`.":
        warn("the sentence does not begin with a capital")
    for label, pattern in MESSAGE_BAD + PROSE_BAD:
        found = pattern.search(sentence)
        if found:
            fail(f"the message has {label}, \"{found.group(0)}\"")
    author, committer, parents = git(repo, "log", "-1", "--format=%an%x00%cn%x00%P", rev).strip().split("\x00")
    if author != AUTHOR or committer != AUTHOR:
        fail(f"author {author} and committer {committer}, where {AUTHOR} is the identity of both")
    # A merge has more than one parent, and a repository's first commit has none.
    if len(parents.split()) > 1:
        fail(f"{len(parents.split())} parents, where a commit on main has one")
    if names:
        shown = git(repo, "show", "--format=%B%n%an%n%ae%n%cn%n%ce", rev)
        count = len(re.findall(names, shown, re.I))
        if count:
            fail(f"{count} hits of a forbidden name")
    else:
        warn("FORBIDDEN_NAMES is not set, so the names were not read for")
    status = [line.split("\t") for line in git(repo, "show", "--format=", "--name-status", rev).splitlines() if line]
    pictures = [p[-1] for p in status if p[0][0] in "AM" and PICTURES.match(p[-1])]
    seen = [p[-1] for p in status if SEEN.search(p[-1]) and not p[-1].startswith(".github/") and "Tests" not in p[-1]]
    if seen and not pictures:
        warn(f"touches what may be seen ({', '.join(seen[:3])}{' ...' if len(seen) > 3 else ''}) and adds no picture")
    for p in status:
        if p[0][0] == "A" and re.search(r"\.(png|jpg|jpeg|hdr|exr|bin|zip|pak)$", p[-1], re.I):
            warn(f"adds a binary file, {p[-1]}")
        if p[0][0] in "AM" and p[-1].startswith(".github/workflows/"):
            note(f"changes a workflow, {p[-1]}")
    for p in pictures:
        picture(repo, rev, p, thumbs, fail, warn, note)

    path, number, in_xml = None, 0, False
    for line in git(repo, "show", "--format=", "--unified=0", rev).splitlines():
        if line.startswith("+++ "):
            path = line[6:] if line.startswith("+++ b/") else None
            in_xml = False
            continue
        hunk = re.match(r"^@@ -\S+ \+(\d+)", line)
        if hunk:
            number = int(hunk.group(1)) - 1
            continue
        if not line.startswith("+") or path is None or not TEXT.search(path):
            continue
        number += 1
        added, where = line[1:], f"{path}:{number}"
        if added.rstrip() != added and not path.endswith(".md"):
            warn(f"{where}: a blank at the line's end")
        # The review's files and the generated devlog are read by the reviewer, not here.
        if path.startswith(".github/") and path.endswith(".md") or path == "DEVLOG.md":
            continue
        # An XML comment runs over lines until its close, and only an MSBuild or XML file has one,
        # so a line of code naming the mark in a string opens none.
        is_comment = bool(COMMENT.match(added)) or in_xml
        if XML.search(path) and (in_xml or added.lstrip().startswith("<!--")):
            in_xml = "-->" not in added
        if not is_comment and not path.endswith(".md"):
            continue
        address = re.sub(r"^\s*(///?|#|<!--|\*)\s*", "", added).strip()
        if is_comment and len(added) > COLUMNS and not re.fullmatch(r"https?://\S+", address):
            fail(f"{where}: a comment line of {len(added)} columns, past {COLUMNS}")
        for label, pattern in PROSE_BAD:
            found = pattern.search(added)
            if found:
                fail(f"{where}: {label}, \"{found.group(0)}\"")
        if path.endswith(".md"):
            continue
        for label, pattern in COMMENT_BAD:
            found = pattern.search(added)
            if found:
                warn(f"{where}: a comment {label}, \"{found.group(0)}\", in {added.strip()[:110]}")
    return sentence, fails, warns, notes


def main():
    arguments = [a for a in sys.argv[1:] if not a.startswith("--")]
    thumbs = next((a.split("=", 1)[1] for a in sys.argv[1:] if a.startswith("--thumbs=")), os.path.join(tempfile.gettempdir(), "commitcheck"))
    if not arguments:
        sys.exit("Usage is commitcheck.py <repo> [<range or commit> ...] [--thumbs=<folder>].")
    repo, revs = arguments[0], arguments[1:] or ["origin/main..HEAD"]
    names = os.environ.get("FORBIDDEN_NAMES")
    if names:
        try:
            re.compile(names)
        except re.error as error:
            sys.exit(f"FORBIDDEN_NAMES is no regular expression, {error}.")
    hashes = []
    for rev in revs:
        hashes += git(repo, "rev-list", "--reverse", rev).split() if ".." in rev else [git(repo, "rev-parse", rev).strip()]
    branch = git(repo, "rev-parse", "--abbrev-ref", "HEAD").strip()
    failed = 0
    if branch.startswith("claude/"):
        print(f"FAIL the branch is {branch}")
        failed += 1
    for commit in hashes:
        sentence, fails, warns, notes = check(repo, commit, names, thumbs)
        print(f"{commit[:8]} {'FAIL' if fails else 'WARN' if warns else 'ok'}  {sentence[:110]}")
        for line in fails:
            print(f"  FAIL {line}")
        for line in warns:
            print(f"  WARN {line}")
        for line in notes:
            print(f"  note {line}")
        failed += bool(fails)
    if not hashes:
        print("no commits in the range")
    sys.exit(1 if failed else 0)


main()
