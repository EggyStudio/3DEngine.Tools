# Proves that scripts/rewrap.py is safe to run over the repositories given, by running it over a
# copy of each alone. Each repository's tracked files of the languages the rewrap reads are copied
# into a folder of their own under the work folder given, made a repository there, and rewrapped
# with --write; the repository itself is read and never written.
#
#   python3 -I scripts/rewrapcheck.py <work folder> <repo> ...
#
# A copied file holds if its code, every line but its comment lines, is byte-equal before and after,
# if its comments' words in their order are the same once their blanks are joined, and if a second
# run of the rewrap changes no file. Comment lines are read here by a rule of this script's own,
# apart from the rewrap's, so a fault of the rewrap's reading is not read the same way twice. It
# prints for each repository the files and lines the rewrap changed and each file where a proof
# does not hold, and the exit code is 1 where any does not.
import os, re, shutil, subprocess, sys

sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")

sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from commitcheck import TEXT, comment_mark  # noqa: E402

C_FAMILY = re.compile(r"\.(cs|rs|slang|wgsl|glsl|hlsl)$")
XML = re.compile(r"\.(csproj|props|targets|slnx|xml)$")


def parts(path, text):
    """A file's code, its lines that are no comment line, and its comments' words in their order.
    A comment line is one whose first mark is its language's, // in the C family, # elsewhere, or
    one inside an XML comment, the forms the rewrap changes."""
    code, said, inside = [], [], False
    for line in text.split("\n"):
        stripped = line.lstrip()
        if XML.search(path):
            if inside or stripped.startswith("<!--"):
                said += line.replace("<!--", " ").replace("-->", " ").split()
                inside = "-->" not in line
                continue
        elif stripped.startswith("//" if C_FAMILY.search(path) else "#") and not stripped.startswith("#!"):
            said += stripped.lstrip("/#").split()
            continue
        code.append(line)
    return "\n".join(code), " ".join(said)


def git(folder, *arguments):
    return subprocess.run(["git", "-C", folder, "-c", "core.autocrlf=false", *arguments], capture_output=True, text=True, check=True).stdout


def rewrap(copy):
    done = subprocess.run([sys.executable, "-I", os.path.join(HERE, "rewrap.py"), "--write", copy], capture_output=True, text=True)
    return done.returncode, done.stdout.strip().splitlines()[-1] if done.stdout.strip() else done.stderr.strip()


def read(folder, paths):
    texts = {}
    for path in paths:
        with open(os.path.join(folder, path), encoding="utf-8", errors="surrogateescape", newline="") as f:
            texts[path] = f.read()
    return texts


def check(work, repo):
    name = os.path.basename(os.path.abspath(repo))
    copy = os.path.join(work, name)
    if os.path.exists(copy):
        shutil.rmtree(copy)
    os.makedirs(copy)
    listed = subprocess.run(["git", "-C", repo, "ls-files", "-z"], capture_output=True, check=True).stdout.decode("utf-8", "replace")
    paths = [p for p in listed.split("\0") if p and os.path.isfile(os.path.join(repo, p)) and TEXT.search(p) and comment_mark(p)]
    for path in paths:
        os.makedirs(os.path.dirname(os.path.join(copy, path)) or copy, exist_ok=True)
        shutil.copyfile(os.path.join(repo, path), os.path.join(copy, path))
    git(copy, "init", "-q")
    git(copy, "add", "-A")

    before = read(copy, paths)
    code, first = rewrap(copy)
    after = read(copy, paths)
    again, second = rewrap(copy)
    last = read(copy, paths)

    faults = []
    if code:
        faults.append(f"the rewrap exited with {code}: {first}")
    changed = [p for p in paths if before[p] != after[p]]
    lines = sum(abs(after[p].count("\n") - before[p].count("\n")) for p in changed)
    for path in changed:
        (code_before, said_before), (code_after, said_after) = parts(path, before[path]), parts(path, after[path])
        if code_before != code_after:
            faults.append(f"{path}: its code is not byte-equal after the rewrap")
        if said_before != said_after:
            faults.append(f"{path}: its comments' words differ after the rewrap")
    for path in paths:
        if after[path] != last[path]:
            faults.append(f"{path}: a second run changed it")
    print(f"{name}: {len(paths)} files copied, {len(changed)} rewrapped with {lines} lines more, {first}")
    print(f"  the second run: {second}")
    for fault in faults:
        print(f"  FAIL {fault}")
    return not faults


def main():
    if len(sys.argv) < 3:
        sys.exit("Usage is rewrapcheck.py <work folder> <repo> ....")
    work, repos = sys.argv[1], sys.argv[2:]
    for repo in repos:
        if os.path.abspath(work).startswith(os.path.abspath(repo) + os.sep):
            sys.exit(f"The work folder lies inside {repo}, which is read and never written.")
    os.makedirs(work, exist_ok=True)
    held = [check(work, repo) for repo in repos]
    sys.exit(0 if all(held) else 1)


main()
