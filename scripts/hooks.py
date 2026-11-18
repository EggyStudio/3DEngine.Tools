# Installs or removes a post-commit hook that runs scripts/commitcheck.py on the commit just made
# and prints its lines, so a contributor sees a FAIL as it commits, before it reports the commit.
#
#   python3 -I scripts/hooks.py install [<repo>]
#   python3 -I scripts/hooks.py remove [<repo>]
#
# The repository is the one the working folder lies in, the folder given or the current one, and a
# repository the working folder does not lie in is refused, so a contributor installs the hook in
# its own repository alone. The hook stops no commit, exits with 0 whatever the check finds, and
# prints nothing where Python or the check's script is missing, so a commit is slowed by the check's
# own run at most. The names the check reads come from FORBIDDEN_NAMES in the committing process's
# environment, which the hook neither reads nor writes. A post-commit hook that is not this script's
# is left as it is and refused.
import os, stat, subprocess, sys

sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")

MARK = "# Written by Tools' scripts/hooks.py."
CHECK = os.path.join(os.path.dirname(os.path.abspath(__file__)), "commitcheck.py")


def git(folder, *arguments):
    done = subprocess.run(["git", "-C", folder, *arguments], capture_output=True, text=True)
    if done.returncode:
        sys.exit(f"git {' '.join(arguments)} failed in {folder}. {done.stderr.strip()}")
    return done.stdout.strip()


def hook(top):
    # The check is named from the repository's top where it lies inside it, as a module's own
    # modules/Tools does, so the hook holds where the checkout is moved.
    inside = os.path.abspath(CHECK).startswith(top + os.sep)
    script = os.path.relpath(CHECK, top).replace(os.sep, "/") if inside else os.path.abspath(CHECK).replace(os.sep, "/")
    located = f'"$(git rev-parse --show-toplevel)/{script}"' if inside else f'"{script}"'
    return f"""#!/bin/sh
{MARK}
# It runs the commit's check on the commit just made and prints its lines, and stops no commit.
script={located}
[ -f "$script" ] || exit 0
for python in python3 python; do
    if command -v "$python" >/dev/null 2>&1; then
        "$python" -I "$script" "$(git rev-parse --show-toplevel)" HEAD 2>&1
        exit 0
    fi
done
exit 0
"""


def main():
    if len(sys.argv) not in (2, 3) or sys.argv[1] not in ("install", "remove"):
        sys.exit("Usage is hooks.py install|remove [<repo>].")
    action, repo = sys.argv[1], sys.argv[2] if len(sys.argv) == 3 else "."
    top = os.path.realpath(git(repo, "rev-parse", "--show-toplevel"))
    here = os.path.realpath(os.getcwd())
    if here != top and not here.startswith(top + os.sep):
        sys.exit(f"{top} is not the repository the working folder lies in, and a contributor installs the hook in its own repository alone.")
    hooks = git(top, "rev-parse", "--git-path", "hooks")
    path = os.path.join(top, hooks, "post-commit") if not os.path.isabs(hooks) else os.path.join(hooks, "post-commit")
    standing = open(path, encoding="utf-8").read() if os.path.exists(path) else None
    if standing is not None and MARK not in standing:
        sys.exit(f"{path} is a post-commit hook of another's, which is left as it is.")
    if action == "remove":
        if standing is not None:
            os.remove(path)
        print(f"removed {path}" if standing is not None else f"no hook of this script's at {path}")
        return
    os.makedirs(os.path.dirname(path), exist_ok=True)
    text = hook(top)
    if standing != text:
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
    os.chmod(path, os.stat(path).st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)
    print(f"installed {path}" if standing != text else f"{path} stands as installed")


main()
