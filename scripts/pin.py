# Moves a module's pin of Tools to a head and writes again what the pin decides, run from the
# module's root by its own contributor.
#
#   python3 -I modules/Tools/scripts/pin.py [<head>] [--tests=<test project>]
#
# It fetches the module's modules/Tools and checks out the head, origin/main where none is given,
# and prints the commits the pin moves by, then reads the table of what a module carries in the
# README of the old pin and of the new one and names each fact new to the module and each whose row
# changed, with the commits that changed it, so the module knows what it owes. Where the module
# keeps a CHEATSHEET.md it runs its test project's cheatsheet fact with CHEATSHEET_WRITE=1, which
# writes the page again, and it writes DEVLOG.md again with the new pin's devlog.py and the day's
# commits. It commits nothing, so its contributor reads the change and commits it. A repository the
# working folder does not lie in is refused. The exit code is 0, or 2 where git, the fetch or dotnet
# could not be reached.
import os, re, subprocess, sys

sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")

# A row of the table, its last cell empty where nothing changed the fact after its form.
ROW = re.compile(r"^\|\s*`(?P<fact>\w+)`\s*\|\s*`(?P<check>[\w.]+)`\s*\|\s*(?P<since>[^|]*?)\s*\|\s*(?P<after>[^|]*?)\s*\|\s*$", re.M)


def stop(sentence):
    print(sentence, file=sys.stderr)
    sys.exit(2)


def run(folder, *arguments, env=None):
    try:
        done = subprocess.run(arguments, cwd=folder, capture_output=True, text=True, env=env)
    except FileNotFoundError as error:
        stop(f"{arguments[0]} could not be run, {error}.")
    if done.returncode:
        stop(f"{' '.join(arguments[:3])} failed in {folder}. {(done.stderr or done.stdout).strip()[-400:]}")
    return done.stdout.strip()


def table(tools, commit):
    """The rows of the table of what a module carries in Tools' README at a commit, by fact."""
    shown = subprocess.run(["git", "-C", tools, "show", f"{commit}:README.md"], capture_output=True, text=True)
    text = shown.stdout if shown.returncode == 0 else ""
    start = text.find("### What a module carries")
    if start < 0:
        return {}
    section = text[start:text.find("\n## ", start) if text.find("\n## ", start) > 0 else len(text)]
    return {row["fact"]: (row["check"], row["since"].strip(), row["after"].strip()) for row in ROW.finditer(section)}


def main():
    arguments = [a for a in sys.argv[1:] if not a.startswith("--")]
    tests = next((a.split("=", 1)[1] for a in sys.argv[1:] if a.startswith("--tests=")), None)
    head = arguments[0] if arguments else "origin/main"
    here = os.path.realpath(os.getcwd())
    root = os.path.realpath(run(here, "git", "rev-parse", "--show-toplevel"))
    tools = os.path.join(root, "modules", "Tools")
    if not os.path.exists(os.path.join(tools, ".git")):
        stop(f"{root} holds no modules/Tools, which pin.py moves, so it is no module that takes Tools.")
    name = os.path.basename(root)

    listed = run(root, "git", "ls-tree", "HEAD", "modules/Tools").split()
    old = listed[2] if len(listed) >= 3 else run(tools, "git", "rev-parse", "HEAD")
    run(tools, "git", "fetch", "-q", "origin")
    new = run(tools, "git", "rev-parse", f"{head}^{{commit}}")
    run(tools, "git", "checkout", "-q", new)
    moved = run(tools, "git", "rev-list", "--count", f"{old}..{new}")
    print(f"Tools moved from {old[:7]} to {new[:7]}, {moved} commit{'' if moved == '1' else 's'}")

    # A row's commits that the old pin holds already change nothing the module owes, so a changed
    # row names the commits it does not hold, and is silent where none are left.
    def held(commit):
        return subprocess.run(["git", "-C", tools, "merge-base", "--is-ancestor", commit, old], capture_output=True).returncode == 0

    before, after = table(tools, old), table(tools, new)
    for fact, (check, since, changed) in after.items():
        if fact not in before:
            print(f"  a new fact, {fact}, calling {check}, in its form since {since}")
            continue
        fresh = [c for c in re.findall(r"`([0-9a-f]{7,40})`", f"{since} {changed}") if c not in " ".join(before[fact]) and not held(c)]
        if before[fact][0] != check or fresh:
            print(f"  a changed fact, {fact}, calling {check}, changed since the pin by {', '.join(f'`{c}`' for c in fresh) or 'its check'}")
    for fact in before:
        if fact not in after:
            print(f"  a fact no longer carried, {fact}")
    if before == after:
        print("  no fact new or changed")

    if os.path.exists(os.path.join(root, "CHEATSHEET.md")):
        project = tests or next((os.path.join(d, f) for d in sorted(os.listdir(root)) if d.endswith(".Tests")
                                 for f in os.listdir(os.path.join(root, d)) if f.endswith(".csproj")), None)
        if project is None:
            print("CHEATSHEET.md is not written again, since no test project ending in .Tests holds the cheatsheet fact; --tests names it")
        else:
            environment = dict(os.environ, CHEATSHEET_WRITE="1", MSBUILDDISABLENODEREUSE="1")
            run(root, "dotnet", "test", project, "--filter", "FullyQualifiedName~Cheatsheet", "-nologo", env=environment)
            changed = run(root, "git", "status", "--porcelain", "--", "CHEATSHEET.md")
            print("CHEATSHEET.md written again" + ("" if changed else ", the same as it stood"))
    else:
        print("no CHEATSHEET.md is kept, so none is written")

    run(root, sys.executable, "-I", os.path.join(tools, "scripts", "devlog.py"), root, name, "--commits")
    print("DEVLOG.md written again by the new pin's devlog.py")
    print("Nothing is committed. Read the change with git status and git diff, then commit it.")


main()
