# Prints a commit's report block, every line of it read by the script and none written by hand, so a
# count in a report is one a reader can read again.
#
#   python3 -I scripts/report.py <repo> [<commit>] [--trx=<results.trx>] [--run]
#
# With --run the block ends with the commit's workflow run on GitHub, its jobs and their times, read
# through gh from the origin's address, or the reason it was not read.
#
# The block gives the commit's hash and sentence, its files by kind and its lines added and removed,
# the facts and the theories' rows of data its diff adds to C# files, as their attributes on added
# lines count them, and the test functions it adds to Python files of tests, the counts of a test
# run read from a TRX file, the one given or the newest under the repository's TestResults folders,
# and the commit's check, scripts/commitcheck.py run on it with the environment's names, its exit
# code and its FAIL and WARN lines counted. A count the script could not read is said to be unread,
# with why. The commit is HEAD where none is given. The exit code is 0, or 2 where git could not
# read the commit.
import datetime, glob, json, os, re, subprocess, sys
import xml.etree.ElementTree as ElementTree

sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")

HERE = os.path.dirname(os.path.abspath(__file__))
# A file's kind by its path, the first that matches.
KINDS = [
    ("pictures", re.compile(r"\.(webp|png|jpe?g)$", re.I)),
    ("workflows", re.compile(r"^\.github/workflows/")),
    # A test project's folder or a folder of tests of any case, Tools.Tests and a tests of Python.
    ("tests", re.compile(r"(^|/)[^/]*tests?/", re.I)),
    ("scripts", re.compile(r"\.(py|sh|ps1)$")),
    ("library", re.compile(r"\.(cs|csproj|props|targets|slnx)$")),
    ("documents", re.compile(r"\.md$")),
]
# A fact's attribute, xunit's own or one deriving from it by name, and a theory's row of data, on an
# added line; a theory's own attribute is not counted, its rows being its cases.
FACT = re.compile(r"^\+\s*\[(\w*Fact)\b")
ROW = re.compile(r"^\+\s*\[(InlineData)\b")
# A test function of Python's unittest or pytest, on an added line of a Python file of tests.
TEST_FUNCTION = re.compile(r"^\+\s*(async\s+)?def test_\w*\s*\(")


def git(repo, *arguments):
    done = subprocess.run(["git", "-C", repo, *arguments], capture_output=True, text=True)
    if done.returncode:
        print(f"git {' '.join(arguments)} failed in {repo}. {done.stderr.strip()}", file=sys.stderr)
        sys.exit(2)
    return done.stdout


def sentence(repo, commit):
    message = git(repo, "cat-file", "commit", commit).split("\n\n", 1)[-1]
    body = message.split("\n\n", 1)[-1] if "\n\n" in message else message
    return " ".join(line.strip() for line in body.splitlines() if line.strip()) or "(no sentence)"


# The outcomes a TRX gives a result, in the words the block says them, passed and failed always
# said.
OUTCOMES = {"Passed": "passed", "Failed": "failed", "NotExecuted": "not executed"}


class Unreached(Exception):
    """gh could not be run or could not answer."""


def gh_json(*arguments):
    try:
        done = subprocess.run(["gh", *arguments], capture_output=True, text=True, timeout=60)
    except (FileNotFoundError, subprocess.TimeoutExpired) as error:
        raise Unreached(f"gh could not be run, {error}")
    if done.returncode:
        lines = done.stderr.strip().splitlines()
        raise Unreached(f"gh {' '.join(arguments[:2])} failed, {lines[-1] if lines else done.returncode}")
    return json.loads(done.stdout or "null")


def commit_run(slug, full):
    """The newest workflow run of the commit on GitHub and its jobs, or None where it has none.
    The tests replace gh by the answers REPORT_ANSWERS names."""
    if os.environ.get("REPORT_ANSWERS"):
        answers = json.load(open(os.environ["REPORT_ANSWERS"], encoding="utf-8"))
        if "failed" in answers:
            raise Unreached(f"gh run list failed, {answers['failed']}")
        return answers.get("run"), answers.get("jobs", [])
    runs = gh_json("run", "list", "-R", slug, "--commit", full, "-L", "1", "--json", "databaseId,status,conclusion")
    if not runs:
        return None, []
    return runs[0], (gh_json("run", "view", str(runs[0]["databaseId"]), "-R", slug, "--json", "jobs") or {}).get("jobs", [])


def took(job):
    """A job's time as run.py gives it, 56 s or 1 min 29 s, or nothing where it has not ended."""
    try:
        start, end = (datetime.datetime.fromisoformat(job[k].replace("Z", "+00:00")) for k in ("startedAt", "completedAt"))
    except (KeyError, TypeError, ValueError):
        return ""
    # A job not yet ended has no conclusion, and gh gives it an end in the year 1.
    if not job.get("conclusion") or end < start:
        return ""
    seconds = round((end - start).total_seconds())
    return f"{seconds // 60} min {seconds % 60} s" if seconds >= 60 else f"{seconds} s"


def run_lines(repo, full):
    """The run's lines of the block, its number, state and jobs, or why it was not read."""
    remote = subprocess.run(["git", "-C", repo, "remote", "get-url", "origin"], capture_output=True, text=True).stdout.strip()
    found = re.search(r"github\.com[:/]([^/]+/[^/]+?)(\.git)?/?$", remote)
    if not found:
        return ["run: not read, the repository has no origin on GitHub"]
    slug = found.group(1)
    try:
        run, jobs = commit_run(slug, full)
    except Unreached as error:
        return [f"run: not read, {error}"]
    if run is None:
        return [f"run: none of {slug} for this commit yet"]
    lines = [f"run: {run['databaseId']} of {slug}, {run.get('status', '')} {run.get('conclusion') or ''}".rstrip()]
    for job in jobs:
        lines.append(f"  {job.get('name')} {job.get('conclusion') or job.get('status', '')} {took(job)}".rstrip())
    return lines


def trx_counts(path):
    """A TRX file's counts and how they were read, by its results' outcomes where it holds results,
    since dotnet test leaves a skipped test out of every counter, or by its counters where not."""
    root = ElementTree.parse(path).getroot()
    results = [e.get("outcome", "") for e in root.iter() if e.tag.endswith("UnitTestResult")]
    counters = next((e for e in root.iter() if e.tag.endswith("Counters")), None)
    if results:
        counts = {"total": str(len(results))}
        for outcome, words in OUTCOMES.items():
            if results.count(outcome) or outcome != "NotExecuted":
                counts[words] = str(results.count(outcome))
        for outcome in sorted(set(results) - set(OUTCOMES)):
            counts[outcome] = str(results.count(outcome))
        return counts, "by its results"
    if counters is None:
        return None, None
    counts = {k: counters.get(k) for k in ("total", "passed", "failed", "notExecuted") if counters.get(k) is not None}
    return counts, "by its counters, holding no results"


def main():
    arguments = [a for a in sys.argv[1:] if not a.startswith("--")]
    trx = next((a.split("=", 1)[1] for a in sys.argv[1:] if a.startswith("--trx=")), None)
    if not arguments:
        sys.exit("Usage is report.py <repo> [<commit>] [--trx=<results.trx>] [--run].")
    repo, commit = arguments[0], arguments[1] if len(arguments) > 1 else "HEAD"
    full = git(repo, "rev-parse", commit).strip()
    lines = [f"`{full[:7]}` {sentence(repo, full)}"]

    stats = [l.split("\t") for l in git(repo, "show", "--format=", "--numstat", full).splitlines() if l.strip()]
    kinds, added, removed = {}, 0, 0
    for number_added, number_removed, path in stats:
        kind = next((k for k, pattern in KINDS if pattern.search(path)), "other")
        kinds[kind] = kinds.get(kind, 0) + 1
        added += int(number_added) if number_added.isdigit() else 0
        removed += int(number_removed) if number_removed.isdigit() else 0
    by_kind = ", ".join(f"{kind} {count}" for kind, count in sorted(kinds.items(), key=lambda item: (-item[1], item[0])))
    lines.append(f"files: {len(stats)} changed, {by_kind}" if stats else "files: none changed")
    lines.append(f"lines: {added} added, {removed} removed")

    facts = rows = functions = 0
    in_cs = in_python_tests = False
    tests = dict(KINDS)["tests"]
    for line in git(repo, "show", "--format=", "--unified=0", full).splitlines():
        if line.startswith("+++ "):
            in_cs = line.endswith(".cs")
            in_python_tests = line.endswith(".py") and bool(tests.search(line[6:]))
        elif in_cs:
            facts += bool(FACT.match(line))
            rows += bool(ROW.match(line))
        elif in_python_tests:
            functions += bool(TEST_FUNCTION.match(line))
    lines.append(f"test attributes on added lines of C#: {facts} facts, {rows} data rows")
    lines.append(f"test functions on added lines of Python tests: {functions}")

    if trx is None:
        found = sorted(glob.glob(os.path.join(repo, "*", "TestResults", "*.trx")) + glob.glob(os.path.join(repo, "TestResults", "*.trx")),
                       key=os.path.getmtime)
        trx = found[-1] if found else None
    if trx is None:
        lines.append("tests: not read, no TRX file is given or under the repository's TestResults")
    else:
        try:
            counts, how = trx_counts(trx)
        except (OSError, ElementTree.ParseError) as error:
            counts, why = None, str(error)
        else:
            why = "it holds no results and no counters"
        if counts:
            lines.append("tests: " + ", ".join(f"{value} {key}" for key, value in counts.items()) + f", read from {os.path.basename(trx)} {how}")
        else:
            lines.append(f"tests: not read from {os.path.basename(trx)}, {why}")

    check = subprocess.run([sys.executable, "-I", os.path.join(HERE, "commitcheck.py"), repo, full], capture_output=True, text=True)
    fails = sum(1 for line in check.stdout.splitlines() if line.lstrip().startswith("FAIL"))
    warns = sum(1 for line in check.stdout.splitlines() if line.lstrip().startswith("WARN"))
    lines.append(f"check: commitcheck.py exit {check.returncode}, {fails} FAIL and {warns} WARN lines")
    if "--run" in sys.argv[1:]:
        lines += run_lines(repo, full)
    print("\n".join(lines))


main()
