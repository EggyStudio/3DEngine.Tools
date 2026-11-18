import subprocess, re, json, sys, os, textwrap
# The line check is commitcheck.py's, beside this script, read without leaving its bytecode.
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from commitcheck import line_faults  # noqa: E402
# What is printed is UTF-8 whatever the system gives a pipe, cp1252 on Windows, so a place or a
# caption holding a dash or a mark reads back whole.
sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")
# --fixes names a file of the caption rewrites, the pattern each repository's STYLE.md adds to the
# check by its name, and the day an undated sent picture is put on, which no repository holds.
args, fixes = sys.argv[1:], {}
# --commits lists each day's commits by their sentences, which a module's devlog reads where its
# review records no item done.
commits = "--commits" in args
args = [a for a in args if a != "--commits"]
if "--fixes" in args:
    at = args.index("--fixes"); fixes = json.load(open(args[at + 1])); del args[at:at + 2]
if len(args) < 2:
    sys.exit("Usage is devlog.py <repo path> <Name> [sent-pictures.json] [--fixes <fixes.json>] [--commits].")
repo, name, sent_json = args[0], args[1], (args[2] if len(args) > 2 else None)
PIC_DIRS = [d for d in (".github/devlog", ".github/assets/readings") if os.path.isdir(os.path.join(repo, d))]
pic_dir = os.path.join(repo, PIC_DIRS[0]) if PIC_DIRS else os.path.join(repo, ".github", "devlog")
def git(*a): return subprocess.run(["git", "-C", repo, *a], capture_output=True, text=True).stdout
fl = lambda s: " ".join(l.strip() for l in s.split("\n"))
extra = fixes.get("patterns", {}).get(name)
BAD = re.compile("\u2014|\u2013|" r"\bjust\b|\bsimply\b|\bwants?\b| now |\bnow\.|\bowner\b|[a-z\)`]: [a-z]" + (f"|{extra}" if extra else ""))
FIX = [tuple(pair) for pair in fixes.get("pairs", [])]
def fix(s):
    for a, b in FIX: s = re.sub(a, b, s)
    return s
uncaptioned = []
days = {}  # date -> {"pictures": [(sortkey, path, caption, hash)], "done": [(num, hash, title)]}
def day(d): return days.setdefault(d, {"pictures": [], "notes": [], "done": [], "commits": []})
in_pic_dir = lambda rel: any(rel.startswith(d + "/") for d in PIC_DIRS)
# captions the sessions wrote, a line a file, the file's name then a bar then one sentence
captions = {}
for d in PIC_DIRS:
    cm = os.path.join(repo, d, "captions.md")
    if os.path.exists(cm):
        for line in open(cm):
            if " | " in line:
                f, c = line.split(" | ", 1); captions[d + "/" + f.strip()] = fix(c.strip())
# pictures sent on the way; those outside the repository are saved under the pictures folder as WebP
if sent_json:
    from PIL import Image
    os.makedirs(pic_dir, exist_ok=True)
    for r in json.load(open(sent_json)):
        d = r["ts"][:10] or fixes.get("undated") or sys.exit(f"A sent picture has no date and no day for one is given: {r['files']}")
        cap = fix(r["caption"])
        for src in r["files"]:
            if not os.path.exists(src): continue
            if os.path.abspath(src).startswith(os.path.abspath(repo) + os.sep):
                captions.setdefault(os.path.relpath(src, repo), cap); cap = ""; continue
            base = os.path.splitext(os.path.basename(src))[0].replace("_", "-"); dst = f"{d}-{base}.webp"
            if not os.path.exists(os.path.join(pic_dir, dst)):
                im = Image.open(src); im = im.convert("RGBA" if im.mode in ("RGBA", "LA", "P") else "RGB")
                small = src.endswith(".png") and os.path.getsize(src) < 200_000 and im.size[0] <= 800
                im.save(os.path.join(pic_dir, dst), "WEBP", lossless=small, quality=85, method=6)
            captions.setdefault(os.path.relpath(pic_dir, repo) + "/" + dst, cap); cap = ""
# the pictures folders, each picture on the day its name says, captioned by the sessions' line
for d in PIC_DIRS:
    for f in sorted(os.listdir(os.path.join(repo, d))):
        if not f.endswith(".webp"): continue
        rel = d + "/" + f; m = re.match(r"(\d{4}-\d{2}-\d{2})", f)
        added = git("log", "--diff-filter=A", "-1", "--format=%cs|%ct|%h", "--", rel).strip().split("|")
        date = m.group(1) if m else (added[0] if added[0] else None)
        if not date: continue
        # A picture's caption is its captions.md line alone, the commit's sentence telling of the
        # work and not the picture, so one with none stands without a caption and is counted.
        cap = captions.get(rel); h = None
        if cap is None: uncaptioned.append(rel)
        when = int(added[1]) if len(added) == 3 else 1 << 40
        day(date)["pictures"].append(((0, when, f), rel, cap or "", h))
# notes the sessions wrote for the devlog, a Markdown file a note beside the pictures, named by the
# day and the item
for d in PIC_DIRS:
    for f in sorted(os.listdir(os.path.join(repo, d))):
        m = re.match(r"(\d{4}-\d{2}-\d{2})-.*\.md$", f)
        if not m or f in ("captions.md", "README.md"): continue
        text = open(os.path.join(repo, d, f)).read().strip("\n")
        if text: day(m.group(1))["notes"].append((f, fix(text)))
# committed captures elsewhere under .github/assets, by the commit that added them
log = git("log", "--reverse", "--diff-filter=A", "--format=@@%cs|%h", "--name-only", "--", ".github/assets/*.webp", ".github/assets/*/*.webp", ".github/assets/*/*/*.webp")
cur = None; order = 0
for line in log.split("\n"):
    if line.startswith("@@"): cur = line[2:].split("|"); order += 1; continue
    if not line.strip() or cur is None or "/gallery/" in line or not line.endswith(".webp") or in_pic_dir(line): continue
    if not os.path.exists(os.path.join(repo, line)): continue
    body = fix(fl(git("log", "-1", "--format=%b", cur[1])).strip())
    day(cur[0])["pictures"].append(((1, order, line), line, body, cur[1]))
# the items done, from the archive and the current review
paras = []
for f in ("REVIEW-ARCHIVE.md", "REVIEW.md"):
    p = os.path.join(repo, ".github", f)
    if os.path.exists(p):
        t = open(p).read(); sec = t.split("## Verdicts")[0] if f == "REVIEW.md" else t.split("## Verdicts settled")[0]
        paras += [(f, q) for q in sec.split("\n\n") if re.match(r"\d+\. \*\*", q)]
seen = set()
for f, q in paras:
    s = fl(q); m = re.match(r"(\d+)\. \*\*(.+?)\*\*", s); num, title = int(m.group(1)), m.group(2)
    if num in seen: continue
    if f == "REVIEW.md" and not re.search(r"[Dd]one at `|item \d+ done|; done\b", s): continue
    hashes = re.findall(r"(?:Done at|Done again at|On main as|Capture done at|done at) `([0-9a-f]{7,8})`", s) or re.findall(r"`([0-9a-f]{7,8})`", s)
    date = None; h = None
    for cand in reversed(hashes):
        d = git("log", "-1", "--format=%cs", cand).strip()
        if d: date, h = d, cand; break
    if not date: continue
    seen.add(num)
    title = re.sub(r",? done at `[0-9a-f]+`[^.]*", "", title).strip()
    title = fix(title).replace(": ", ", ").rstrip(";., ") + "."
    day(date)["done"].append((num, h, title))
# the commits by their sentences, a message's first line under its subject or its two where it has
# two, those that change the review's files or the devlog alone left out
OWN = {".github/REVIEW.md", ".github/REVIEW-ARCHIVE.md", ".github/NORM.md", "DEVLOG.md"}
# A sentence naming an engine outside the form of a module's assembly, 3DEngine.Name, or one that
# would fail the line check an added line of the page gets, is left out of the page a reader sees,
# and only counted on the output, since pushed history is not rewritten. "The bridge" stood for an
# engine in the modules' old sentences, so here alone it is read as naming one.
ENGINE = re.compile(r"3DEngine(?!\.[A-Za-z])|\bBevy(CSharp)?\b|\bE3D\b|\bthe bridge\b", re.I)
left_out = failing = 0
if commits:
    for entry in git("log", "--format=@@%h%x00%cs%x00%B%x00", "--name-only").split("@@")[1:]:
        h, d, body, files = (entry.split("\x00") + ["", "", "", ""])[:4]
        changed = {f for f in files.split("\n") if f.strip()}
        if not changed or changed <= OWN: continue
        said = [l.strip() for l in body.split("\n\n", 1)[-1].split("\n") if l.strip()][:2] if "\n\n" in body else []
        if not said:
            continue
        line = f"- `{h}` {fix(' '.join(said))}"
        if ENGINE.search(line):
            left_out += 1
        # The check leaves DEVLOG.md unread, being written from the history, so the line is read
        # as a line of any other page.
        elif any(level == "FAIL" for level, _, _ in line_faults("page.md", line, False)[0]):
            failing += 1
        else:
            day(d)["commits"].append((h, fix(" ".join(said))))
# write
folder = ", ".join(f"`{d}`" for d in PIC_DIRS)
intro = (f"What landed in {name}, by day and newest first, with the pictures taken on the way, the captures the repository "
 "keeps under `.github/assets`" + (f" and the pictures made while an item was built, kept under {folder} as WebP files named by their day, each on the day its name says, and the notes written beside them as Markdown files named the same way" if PIC_DIRS else "")
 + ". A line under Done names the item of .github/REVIEW.md or .github/REVIEW-ARCHIVE.md and the commit that closed it.")
lines = [f"# {name} devlog", ""] + textwrap.wrap(intro, 100, break_on_hyphens=False) + [""]
for d in sorted(days, reverse=True):
    e = days[d]
    if not e["pictures"] and not e["done"] and not e["notes"] and not e["commits"]: continue
    lines += [f"## {d}", ""]
    groups = []
    for key, path, cap, h in sorted(e["pictures"], key=lambda r: r[0]):
        if groups and h is not None and groups[-1][2] == h and groups[-1][1] == cap: groups[-1][0].append(path)
        else: groups.append(([path], cap, h))
    for paths, cap, h in groups:
        for path in paths: lines.append(f"![]({path})")
        if cap: lines += textwrap.wrap(cap + (f" (`{h}`)" if h else ""), 100)
        lines.append("")
    for f, text in sorted(e["notes"]):
        lines += text.split("\n") + [""]
    if e["done"]:
        lines.append("Done that day"); lines.append("")
        for num, h, title in sorted(e["done"]):
            lines += textwrap.wrap(f"- Item {num} at `{h}`. {title}", 100, subsequent_indent="  ")
        lines.append("")
    if e["commits"]:
        lines.append("Commits that day"); lines.append("")
        for h, said in e["commits"]:
            lines += textwrap.wrap(f"- `{h}` {said}", 100, subsequent_indent="  ")
        lines.append("")
text = "\n".join(lines).rstrip("\n") + "\n"
open(os.path.join(repo, "DEVLOG.md"), "w").write(text)
bad = [l for l in text.split("\n") if BAD.search(l) and not l.startswith("![")]
print(name, "days", len([d for d in days if days[d]["pictures"] or days[d]["done"] or days[d]["commits"]]), "pictures", sum(len(e["pictures"]) for e in days.values()), "done", sum(len(e["done"]) for e in days.values()), "commits", sum(len(e["commits"]) for e in days.values()), "bad lines", len(bad))
for l in bad[:12]: print("  BAD:", l[:140])
if uncaptioned:
    print(f"{len(uncaptioned)} picture with no caption, shown without one" if len(uncaptioned) == 1 else f"{len(uncaptioned)} pictures with no caption, shown without one")
if left_out:
    print(f"{left_out} commit left out, its sentence naming an engine" if left_out == 1 else f"{left_out} commits left out, their sentences naming an engine")
if failing:
    print(f"{failing} commit left out, its sentence failing the line check" if failing == 1 else f"{failing} commits left out, their sentences failing the line check")
