# Prints a line a repository for the hourly watch, reading each and writing nothing: its local and
# remote heads and how many commits it is ahead, its last build run's head, status and conclusion,
# the size of its .github/REVIEW.md where it passes 80 KB, with scripts/archive.py named to move it,
# whether DEVLOG.md was last committed before its newest picture, its pin of Tools against Tools'
# head, and whether its README opens with the link row.
#
#   python3 -I scripts/status.py [--tools=<Tools checkout>] [<repo> ...]
#
# With no repository given it reads 3DEngine, BevyCSharp and 3DEngine/Voxel-Sandbox beside the
# Modules folder this checkout stands in, and every folder of Modules. The remote head is read by
# git ls-remote, which fetches nothing, and the runs through gh, so the login gh holds serves; where
# STATUS_ANSWERS names a JSON file of recorded runs by account and repository, those are read
# instead, a run, null for none or an object naming why gh failed, which is how the tests run it.
# The exit code is 0, or 2 where git or gh could not be reached for a repository, its line saying
# which.
import json, os, re, subprocess, sys
from concurrent.futures import ThreadPoolExecutor

sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REVIEW_LIMIT = 80 * 1024
PICTURES = (".github/assets/readings", ".github/devlog")
GITHUB = re.compile(r"github\.com[:/](?P<slug>[^/]+/[^/]+?)(\.git)?/?$")


class Unreached(Exception):
    pass


def git(repo, *arguments):
    try:
        done = subprocess.run(["git", "-C", repo, *arguments], capture_output=True, text=True, timeout=60)
    except (FileNotFoundError, subprocess.TimeoutExpired) as error:
        raise Unreached(f"git could not be run, {error}")
    if done.returncode:
        raise Unreached(f"git {arguments[0]} failed, {done.stderr.strip().splitlines()[-1] if done.stderr.strip() else done.returncode}")
    return done.stdout.strip()


def last_run(slug):
    """The newest build run of a repository on GitHub as gh gives it, or None where it has none.
    The tests replace gh by the recorded runs STATUS_ANSWERS names."""
    if os.environ.get("STATUS_ANSWERS"):
        answer = json.load(open(os.environ["STATUS_ANSWERS"], encoding="utf-8")).get(slug)
        if isinstance(answer, dict) and "failed" in answer:
            raise Unreached(f"gh run list failed, {answer['failed']}")
        return answer
    try:
        done = subprocess.run(["gh", "run", "list", "-R", slug, "-L", "1", "--json", "headSha,status,conclusion"],
                              capture_output=True, text=True, timeout=60)
    except (FileNotFoundError, subprocess.TimeoutExpired) as error:
        raise Unreached(f"gh could not be run, {error}")
    if done.returncode:
        raise Unreached(f"gh run list failed, {done.stderr.strip().splitlines()[-1] if done.stderr.strip() else done.returncode}")
    runs = json.loads(done.stdout or "[]")
    return runs[0] if runs else None


def committed(repo, *paths):
    stamp = git(repo, "log", "-1", "--format=%ct", "--", *paths)
    return int(stamp) if stamp else None


def line(repo, tools_head, tools):
    name = os.path.basename(os.path.abspath(repo))
    parts, unreached = [name], []
    try:
        head = git(repo, "rev-parse", "--short=7", "HEAD")
        branch = git(repo, "rev-parse", "--abbrev-ref", "HEAD")
        parts.append(f"local {head}")
        url = git(repo, "config", "--get", "remote.origin.url")
        listed = git(repo, "ls-remote", "origin", f"refs/heads/{branch}")
        remote = listed.split()[0] if listed else ""
        if remote:
            parts.append(f"remote {remote[:7]}")
            known = subprocess.run(["git", "-C", repo, "cat-file", "-e", remote], capture_output=True).returncode == 0
            parts.append(f"ahead {git(repo, 'rev-list', '--count', f'{remote}..HEAD')}" if known else "ahead ? (the remote head is not fetched)")
        else:
            parts.append("remote none")
    except Unreached as error:
        return "  ".join(parts + [f"git unreached, {error}"]), True

    found = GITHUB.search(url)
    if found:
        try:
            run = last_run(found.group("slug"))
            parts.append(f"run {run['headSha'][:7]} {run['status']} {run.get('conclusion') or ''}".rstrip() if run else "run none")
        except Unreached as error:
            parts.append(f"run unreached, {error}")
            unreached.append(error)

    review = os.path.join(repo, ".github", "REVIEW.md")
    if os.path.exists(review) and os.path.getsize(review) > REVIEW_LIMIT:
        parts.append(f"review {os.path.getsize(review) // 1024} KB, archive.py moves its older replies")

    # A picture committed after the devlog was last written stands in no devlog yet.
    folders = [p for p in PICTURES if os.path.isdir(os.path.join(repo, p))]
    pictures = committed(repo, *folders) if folders else None
    devlog = committed(repo, "DEVLOG.md")
    if pictures and (devlog is None or devlog < pictures):
        parts.append("devlog behind its newest picture")

    pin = subprocess.run(["git", "-C", repo, "ls-tree", "HEAD", "modules/Tools"], capture_output=True, text=True).stdout.split()
    if len(pin) >= 3 and pin[1] == "commit":
        if tools_head is None:
            parts.append(f"Tools at {pin[2][:7]}")
        elif pin[2].startswith(tools_head) or tools_head.startswith(pin[2][:7]):
            parts.append("Tools at its head")
        else:
            behind = subprocess.run(["git", "-C", tools, "rev-list", "--count", f"{pin[2]}..HEAD"], capture_output=True, text=True)
            parts.append(f"Tools at {pin[2][:7]}, {behind.stdout.strip()} behind" if behind.returncode == 0 else f"Tools at {pin[2][:7]}, not in Tools' history")

    readme = os.path.join(repo, "README.md")
    first = open(readme, encoding="utf-8", errors="replace").readline().rstrip("\n") if os.path.exists(readme) else ""
    slug = found.group("slug") if found else f"EggyStudio/{name}"
    row = f"[Devlog](https://github.com/{slug}/blob/main/DEVLOG.md) [Cheatsheet](https://github.com/{slug}/blob/main/CHEATSHEET.md)"
    parts.append("row ok" if first == row else "row missing")
    return "  ".join(parts), bool(unreached)


def main():
    tools = next((a.split("=", 1)[1] for a in sys.argv[1:] if a.startswith("--tools=")), HERE)
    repos = [a for a in sys.argv[1:] if not a.startswith("--")]
    if not repos:
        modules = os.path.dirname(HERE)
        projects = os.path.dirname(modules)
        repos = [os.path.join(projects, r) for r in ("3DEngine", "BevyCSharp", os.path.join("3DEngine", "Voxel-Sandbox"))]
        repos += sorted(os.path.join(modules, d) for d in os.listdir(modules) if os.path.isdir(os.path.join(modules, d, ".git")) or os.path.isfile(os.path.join(modules, d, ".git")))
    try:
        tools_head = git(tools, "rev-parse", "HEAD")
    except Unreached:
        tools_head = None
    # Each repository waits on the network for its remote and its runs, so they are read at once
    # and printed in the order given.
    unreached = False
    with ThreadPoolExecutor(max_workers=8) as pool:
        for text, missed in pool.map(lambda repo: line(repo, tools_head, tools), repos):
            print(text)
            unreached |= missed
    sys.exit(2 if unreached else 0)


main()
