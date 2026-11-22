# Prints a line a repository for the hourly watch, reading each and writing nothing: its local and
# remote heads and how many commits it is ahead, its last build run's head, status and conclusion,
# the size of its .github/REVIEW.md where it passes 80 KB, with scripts/archive.py named to move it,
# whether DEVLOG.md was last committed before its newest picture, its pin of Tools against Tools'
# head, and whether its README opens with the link row. After them it prints the lines of
# gpu.py's status, each holder of the GPU's lock and each mark of those waiting, with its age.
#
#   python3 -I scripts/status.py [--tools=<Tools checkout>] [--lock=<file>] [--waiting=<folder>]
#       [--timeout=<seconds>] [<repo> ...]
#
# With no repository given it reads 3DEngine, BevyCSharp and 3DEngine/Voxel-Sandbox beside the
# Modules folder this checkout stands in, and every folder of Modules. The remote head is read by
# git ls-remote, which fetches nothing, and the runs through gh, so the login gh holds serves; where
# STATUS_ANSWERS names a JSON file of recorded runs by account and repository, those are read
# instead, a run, null for none or an object naming why gh failed, which is how the tests run it.
# Every repository's calls over the network, its remote's head and its last run, are made at once,
# each waited on for 30 seconds or what --timeout gives, and a call that passes it is said on its
# repository's own line, git ls-remote or gh run list timed out. The lines keep the order given.
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


# How long a call of git or gh is waited on, in seconds, which --timeout sets.
TIMEOUT = 30.0


def git(repo, *arguments):
    try:
        done = subprocess.run(["git", "-C", repo, *arguments], capture_output=True, text=True, timeout=TIMEOUT)
    except subprocess.TimeoutExpired:
        raise Unreached(f"git {arguments[0]} timed out after {TIMEOUT:g} s")
    except FileNotFoundError as error:
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
                              capture_output=True, text=True, timeout=TIMEOUT)
    except subprocess.TimeoutExpired:
        raise Unreached(f"gh run list timed out after {TIMEOUT:g} s")
    except FileNotFoundError as error:
        raise Unreached(f"gh could not be run, {error}")
    if done.returncode:
        raise Unreached(f"gh run list failed, {done.stderr.strip().splitlines()[-1] if done.stderr.strip() else done.returncode}")
    runs = json.loads(done.stdout or "[]")
    return runs[0] if runs else None


def committed(repo, *paths):
    stamp = git(repo, "log", "-1", "--format=%ct", "--", *paths)
    return int(stamp) if stamp else None


def begin(repo, pool):
    """Starts a repository's two calls over the network, its remote's head and its last run, at
    once and with every other repository's, and returns their futures by name, none where its local
    reading failed, which its line then says."""
    try:
        branch = git(repo, "rev-parse", "--abbrev-ref", "HEAD")
        url = git(repo, "config", "--get", "remote.origin.url")
    except Unreached:
        return {}
    started = {"ls": pool.submit(git, repo, "ls-remote", "origin", f"refs/heads/{branch}")}
    found = GITHUB.search(url)
    if found:
        started["run"] = pool.submit(last_run, found.group("slug"))
    return started


def line(repo, tools_head, tools, started):
    name = os.path.basename(os.path.abspath(repo))
    parts, unreached = [name], []
    try:
        head = git(repo, "rev-parse", "--short=7", "HEAD")
        branch = git(repo, "rev-parse", "--abbrev-ref", "HEAD")
        parts.append(f"local {head}")
        url = git(repo, "config", "--get", "remote.origin.url")
        listed = started["ls"].result() if "ls" in started else git(repo, "ls-remote", "origin", f"refs/heads/{branch}")
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
            run = started["run"].result() if "run" in started else last_run(found.group("slug"))
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
    # A module's repository is named 3DEngine and its folder's name, the engines and the game
    # theirs.
    module = os.path.basename(os.path.dirname(os.path.abspath(repo))) == "Modules"
    slug = found.group("slug") if found else f"EggyStudio/3DEngine.{name}" if module else f"EggyStudio/{name}"
    row = f"[Devlog](https://github.com/{slug}/blob/main/DEVLOG.md) [Cheatsheet](https://github.com/{slug}/blob/main/CHEATSHEET.md)"
    parts.append("row ok" if first == row else "row missing")
    return "  ".join(parts), bool(unreached)


def gpu_lines(lock, waiting):
    """The lines of gpu.py's status, the lock's holders and each mark with its age, as the gate
    reads them. Windows has no flock, so the gate is not run there."""
    if sys.platform == "win32":
        return []
    given = [f"--lock={lock}"] * bool(lock) + [f"--waiting={waiting}"] * bool(waiting)
    try:
        done = subprocess.run([sys.executable, "-I", os.path.join(HERE, "scripts", "gpu.py"), *given, "status"], capture_output=True, text=True, timeout=60)
    except (OSError, subprocess.TimeoutExpired) as error:
        return [f"gpu: not read, {error}"]
    return done.stdout.splitlines() if done.returncode == 0 else [f"gpu: not read, gpu.py exit {done.returncode}"]


def main():
    tools = next((a.split("=", 1)[1] for a in sys.argv[1:] if a.startswith("--tools=")), HERE)
    waiting = next((a.split("=", 1)[1] for a in sys.argv[1:] if a.startswith("--waiting=")), None)
    lock = next((a.split("=", 1)[1] for a in sys.argv[1:] if a.startswith("--lock=")), None)
    global TIMEOUT
    TIMEOUT = float(next((a.split("=", 1)[1] for a in sys.argv[1:] if a.startswith("--timeout=")), TIMEOUT))
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
    with ThreadPoolExecutor(max_workers=max(8, 2 * len(repos))) as network, ThreadPoolExecutor(max_workers=8) as pool:
        started = [begin(repo, network) for repo in repos]
        for text, missed in pool.map(lambda pair: line(pair[0], tools_head, tools, pair[1]), zip(repos, started)):
            print(text)
            unreached |= missed
    for text in gpu_lines(lock, waiting):
        print(text)
    sys.exit(2 if unreached else 0)


main()
