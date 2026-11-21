# The gate a module's main is pushed through, so a push waits for the build run before it, stops
# at a fault of the form, and is never forced.
#
#   python3 -I scripts/push.py <repository path> <account/repository> <hash> [--branch main]
#       [--wait-minutes <n>] [--poll-seconds <n>] [--over-red] [--dry-run]
#
# It runs commitcheck.py on the commits from the remote branch to the hash and stops on a FAIL,
# printing the check's lines. It reads the latest run of the remote branch's head through gh, and
# waits while it is queued or in progress, asking every 30 seconds, or --poll-seconds, up to 60
# minutes, or --wait-minutes. A head with no run at all is not waited on: the branch is read back
# from the head for the newest commit that has a run, up to 100 commits, and that run's state is
# the one taken. A run that has not succeeded stops the push, unless --over-red is given. Then it
# runs git push --no-verify origin <hash>:<branch> and prints the remote's head. The push is
# never forced, so a remote that has moved is refused by git and its line printed. --dry-run does
# all of it but the push, which git is asked to rehearse.
#
# The exit code is 0 for a push made or rehearsed, 1 for a call that is wrong, 2 for a FAIL of the
# check, 3 where the wait passed its bound, 4 for a run that did not succeed, its address printed,
# 5 for a push git refused as not a fast-forward, with git's line, and 6 where git or gh could not
# be reached. Where PUSH_ANSWERS names a JSON file, the runs are read from its "runs", each commit's
# hash giving a run, null for none, or a list of runs, one for each ask and the last repeated, or
# from its "failed" as the reason gh failed, which is how the tests run it.
import json, os, subprocess, sys, time

sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")

HERE = os.path.dirname(os.path.abspath(__file__))
LOOK_BACK = 100
# What a run's status says while it is still to end, and the conclusions that let a push go.
GOING = {"queued", "in_progress", "waiting", "requested", "pending"}
PASSED = {"success", "neutral", "skipped"}


class Stop(Exception):
    def __init__(self, code, text):
        super().__init__(text)
        self.code, self.text = code, text


def git(repo, *arguments, allow=()):
    try:
        done = subprocess.run(["git", "-C", repo, *arguments], capture_output=True, text=True)
    except FileNotFoundError as error:
        raise Stop(6, f"git could not be run, {error}.")
    if done.returncode and done.returncode not in allow:
        raise Stop(6, f"git {' '.join(arguments[:2])} failed in {repo}. {(done.stderr or done.stdout).strip()}")
    return done


class Runs:
    """The run reader. gh gives a commit's newest run, and the tests' answers stand in for it."""

    def __init__(self):
        self.asked = {}
        self.answers = json.load(open(os.environ["PUSH_ANSWERS"], encoding="utf-8")) if os.environ.get("PUSH_ANSWERS") else None

    def of(self, slug, commit):
        """The newest run of the commit as gh names it, or None where it has none."""
        if self.answers is not None:
            if "failed" in self.answers:
                raise Stop(6, f"gh run list failed, {self.answers['failed']}")
            answer = self.answers.get("runs", {}).get(commit)
            if isinstance(answer, list):
                number = self.asked.get(commit, 0)
                self.asked[commit] = number + 1
                return answer[min(number, len(answer) - 1)]
            return answer
        try:
            done = subprocess.run(["gh", "run", "list", "-R", slug, "--commit", commit, "-L", "1", "--json", "databaseId,status,conclusion,url"],
                                  capture_output=True, text=True, timeout=60)
        except (FileNotFoundError, subprocess.TimeoutExpired) as error:
            raise Stop(6, f"gh could not be run, {error}.")
        if done.returncode:
            lines = done.stderr.strip().splitlines()
            raise Stop(6, f"gh run list failed, {lines[-1] if lines else done.returncode}")
        runs = json.loads(done.stdout or "[]")
        return runs[0] if runs else None


def newest_run(runs, slug, repo, branch, head):
    """The run to read for the branch's head, the head's own or that of the newest commit back
    along the branch that has one, with the commit it belongs to, or None, None where no commit of
    the last hundred has one."""
    run = runs.of(slug, head)
    if run:
        return run, head
    older = git(repo, "rev-list", f"--max-count={LOOK_BACK}", f"origin/{branch}").stdout.split()
    for commit in older[1:]:
        run = runs.of(slug, commit)
        if run:
            print(f"{head[:7]} has no run, the newest back with one is {commit[:7]}")
            return run, commit
    return None, None


def address(slug, run):
    return run.get("url") or f"https://github.com/{slug}/actions/runs/{run.get('databaseId')}"


def gate(repo, slug, target, branch, wait_minutes, poll_seconds, over_red, dry_run):
    listed = git(repo, "ls-remote", "origin", f"refs/heads/{branch}").stdout.split()
    head = listed[0] if listed else None
    if head:
        git(repo, "fetch", "-q", "origin", f"+refs/heads/{branch}:refs/remotes/origin/{branch}")
    full = git(repo, "rev-parse", "--verify", f"{target}^{{commit}}").stdout.strip()

    checked = subprocess.run([sys.executable, "-I", os.path.join(HERE, "commitcheck.py"), repo, f"origin/{branch}..{full}" if head else full],
                             capture_output=True, text=True)
    lines = checked.stdout.splitlines()
    if checked.returncode:
        print("\n".join(lines))
        raise Stop(2, f"the check of the commits to push stopped it, exit {checked.returncode}")
    warns = [line for line in lines if line.lstrip().startswith("WARN")]
    print(f"check: commitcheck.py exit 0, {len(warns)} WARN line{'' if len(warns) == 1 else 's'}")

    if head:
        runs, begun, told = Runs(), time.monotonic(), None
        while True:
            run, commit = newest_run(runs, slug, repo, branch, head)
            if run is None:
                print(f"run: none of the last {LOOK_BACK} commits of {branch} has one, nothing to wait for")
                break
            state = run.get("status") or ""
            if state in GOING:
                if time.monotonic() - begun >= wait_minutes * 60:
                    raise Stop(3, f"run {run.get('databaseId')} of {commit[:7]} is still {state.replace('_', ' ')} after {wait_minutes:g} min, {address(slug, run)}")
                if told != run.get("databaseId"):
                    print(f"waits for run {run.get('databaseId')} of {commit[:7]}, {state.replace('_', ' ')}")
                    told = run.get("databaseId")
                time.sleep(poll_seconds)
                continue
            conclusion = run.get("conclusion") or state
            print(f"run {run.get('databaseId')} of {commit[:7]}, {conclusion}")
            if conclusion not in PASSED and not over_red:
                raise Stop(4, f"run {run.get('databaseId')} of {commit[:7]} did not succeed, {conclusion}, {address(slug, run)}")
            break

    # Never forced: a remote that has moved is refused by git as it stands.
    pushed = git(repo, "push", "--no-verify", *(["--dry-run"] if dry_run else []), "origin", f"{full}:refs/heads/{branch}", allow=(1,))
    if pushed.returncode:
        text = (pushed.stderr or pushed.stdout).strip()
        refused = [line for line in text.splitlines() if "rejected" in line or "non-fast-forward" in line or "fetch first" in line]
        raise Stop(5 if refused else 6, "\n".join(refused) if refused else f"git push failed. {text}")
    if dry_run:
        print(f"dry run, nothing pushed, {full[:7]} would be {branch}'s head")
        return
    now = git(repo, "ls-remote", "origin", f"refs/heads/{branch}").stdout.split()
    print(f"remote {branch} now {now[0][:7] if now else 'unread'}")


def main():
    arguments = sys.argv[1:]
    options = {"branch": "main", "wait-minutes": "60", "poll-seconds": "30"}
    flags = {"over-red": False, "dry-run": False}
    positional = []
    # An option's value follows its name after an equals sign or as the next argument.
    while arguments and positional is not None:
        argument = arguments.pop(0)
        if not argument.startswith("--"):
            positional.append(argument)
            continue
        key, equals, value = argument[2:].partition("=")
        if key in flags and not equals:
            flags[key] = True
        elif key in options and (equals or arguments):
            options[key] = value if equals else arguments.pop(0)
        else:
            positional = None
    try:
        wait, poll = float(options["wait-minutes"]), float(options["poll-seconds"])
    except ValueError:
        positional = None
    if positional is None or len(positional) != 3 or wait <= 0 or poll <= 0:
        sys.stderr.write("Usage is push.py <repository path> <account/repository> <hash> [--branch=main] [--wait-minutes=60] [--poll-seconds=30] [--over-red] [--dry-run].\n")
        return 1
    try:
        gate(positional[0], positional[1], positional[2], options["branch"], wait, poll, flags["over-red"], flags["dry-run"])
    except Stop as stop:
        print(stop.text, file=sys.stderr if stop.code != 2 else sys.stdout)
        return stop.code
    return 0


sys.exit(main())
