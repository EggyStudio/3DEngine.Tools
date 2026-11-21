# The one gate a GPU's program is run through, N 7.9's lock taken in one way by every caller, so
# no hand-written gate lets a timing wait behind a suite that asked after it.
#
#   python3 -I scripts/gpu.py shared [--minutes <n>] -- <command> [-- <command> ...]
#   python3 -I scripts/gpu.py exclusive [--minutes <n>] -- <command>
#   python3 -I scripts/gpu.py status
#
# The lock is /run/user/1000/gpu.lock and the marks stand in /run/user/1000/gpu.waiting, or the
# file and folder --lock= and --waiting= give before the word. An exclusive caller puts its mark,
# <repository>-<pid>, and waits in flock itself, so it takes the lock the moment the last shared
# holder lets go, and removes its mark on taking it or on its end, however it ends. A shared caller
# starts no part while a mark of a living process stands, removes a mark whose process is gone, and
# asks the folder again before each part, so a run given in parts yields to a timing between them.
# Each run or part is bounded, an exclusive one at three minutes and a shared one at ten, or at the
# minutes --minutes states, and is stopped with every program it started past its bound. The exit
# code is the last command's, 124 where one was stopped past its bound, or 2 where the call was
# wrong. Linux and macOS run it; Windows has no flock.
import os, re, signal, subprocess, sys, time

try:
    import fcntl
except ImportError:
    sys.exit("gpu.py runs where flock does, on Linux and macOS.")

sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")

LOCK = "/run/user/1000/gpu.lock"
WAITING = "/run/user/1000/gpu.waiting"
BOUNDS = {"exclusive": 3.0, "shared": 10.0}
# How often a shared caller asks the folder and the lock again while it waits.
POLL_SECONDS = 1.0
# How long a stopped run's programs are given to end before they are killed.
GRACE_SECONDS = 10


def say(text):
    print(f"gpu.py: {text}", file=sys.stderr, flush=True)


def lives(pid):
    """Whether a process lives, its signal 0 answered or refused, but not unknown."""
    try:
        os.kill(pid, 0)
    except ProcessLookupError:
        return False
    except PermissionError:
        return True
    return True


def marks(folder):
    """The folder's marks, each its name, its pid or None where its name gives none, and its age in
    minutes."""
    found = []
    try:
        names = sorted(os.listdir(folder))
    except OSError:
        return found
    for name in names:
        try:
            age = (time.time() - os.path.getmtime(os.path.join(folder, name))) / 60
        except OSError:
            continue
        pid = name.rpartition("-")[2]
        found.append((name, int(pid) if pid.isdigit() else None, age))
    return found


def standing(folder):
    """The marks of living processes, a mark whose process is gone removed and said, since a stale
    mark would hold every shared run back for good. A mark whose name gives no pid stands."""
    living = []
    for name, pid, age in marks(folder):
        if pid is not None and not lives(pid):
            try:
                os.remove(os.path.join(folder, name))
                say(f"removed the mark {name}, its process gone")
            except FileNotFoundError:
                pass
            continue
        living.append(name)
    return living


def repository():
    """The repository the caller works in, its checkout's folder name, or the working folder's."""
    done = subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True)
    folder = done.stdout.strip() if done.returncode == 0 and done.stdout.strip() else os.getcwd()
    return re.sub(r"[^\w.]+", "_", os.path.basename(folder.rstrip("/"))) or "unknown"


def run_bounded(command, minutes):
    """Runs a command in a process group of its own and returns its exit code, or 124 where it
    passed its bound. Every program left in the group at its end is stopped, since a program left
    running keeps the GPU busy after the lock is let go, as a launcher that returns does."""
    try:
        child = subprocess.Popen(command, start_new_session=True)
    except OSError as error:
        say(f"{command[0]} could not be started, {error}")
        return 127
    code = None
    try:
        code = child.wait(timeout=minutes * 60)
    except subprocess.TimeoutExpired:
        say(f"stopped past its bound of {minutes:g} min, {' '.join(command)}")
    finally:
        stop_group(child)
    return 124 if code is None else code


def stop_group(child):
    """Ends a command's process group, gently and then by force."""
    try:
        os.killpg(child.pid, signal.SIGTERM)
    except (ProcessLookupError, PermissionError):
        return
    end = time.time() + GRACE_SECONDS
    while time.time() < end:
        if child.poll() is None:
            time.sleep(0.1)
            continue
        try:
            os.killpg(child.pid, 0)
        except (ProcessLookupError, PermissionError):
            return
        time.sleep(0.1)
    try:
        os.killpg(child.pid, signal.SIGKILL)
    except (ProcessLookupError, PermissionError):
        pass
    child.wait()


def open_lock(path):
    # Python opens a file not inherited by its children, so the lock is held by this process alone
    # and is let go when it ends, whatever its programs do.
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    return os.open(path, os.O_RDWR | os.O_CREAT, 0o666)


def exclusive(lock, folder, minutes, command):
    os.makedirs(folder, exist_ok=True)
    mark = os.path.join(folder, f"{repository()}-{os.getpid()}")
    with open(mark, "w", encoding="utf-8") as file:
        file.write(" ".join(command) + "\n")
    descriptor = open_lock(lock)
    try:
        say(f"waits for the lock exclusive, its mark {os.path.basename(mark)}")
        fcntl.flock(descriptor, fcntl.LOCK_EX)
        os.remove(mark)
        say("took the lock exclusive")
        return run_bounded(command, minutes)
    finally:
        try:
            os.remove(mark)
        except FileNotFoundError:
            pass
        os.close(descriptor)


def shared(lock, folder, minutes, parts):
    code = 0
    for command in parts:
        descriptor = open_lock(lock)
        try:
            said = None
            while True:
                living = standing(folder)
                if not living:
                    try:
                        fcntl.flock(descriptor, fcntl.LOCK_SH | fcntl.LOCK_NB)
                    except BlockingIOError:
                        living = ["the lock held exclusive"]
                    else:
                        # A mark put between the folder's reading and the lock's taking goes first.
                        if not standing(folder):
                            break
                        fcntl.flock(descriptor, fcntl.LOCK_UN)
                        continue
                text = "the lock held exclusive" if living == ["the lock held exclusive"] else f"{len(living)} mark{'s' * (len(living) != 1)}, {', '.join(living)}"
                if text != said:
                    say(f"waits for {text}")
                    said = text
                time.sleep(POLL_SECONDS)
            code = run_bounded(command, minutes)
        finally:
            os.close(descriptor)
        if code:
            return code
    return code


def holders(lock):
    """The lock's holders as Linux lists them in /proc/locks, each its kind and pid, or None where
    the system lists no locks."""
    try:
        listed = open("/proc/locks", encoding="utf-8").read().splitlines()
    except OSError:
        return None
    try:
        status = os.stat(lock)
    except OSError:
        return []
    inode = f"{os.major(status.st_dev):02x}:{os.minor(status.st_dev):02x}:{status.st_ino}"
    found = []
    for line in listed:
        fields = line.split()
        # 1: FLOCK  ADVISORY  WRITE 4242 00:2b:1234 0 EOF, a waiting lock's line marked with ->.
        if "->" in fields or len(fields) < 6 or fields[1] != "FLOCK" or fields[5] != inode:
            continue
        found.append(("exclusive" if fields[3] == "WRITE" else "shared", int(fields[4])))
    return found


def process_minutes(pid):
    """How long a process has run, read from /proc, or None."""
    try:
        start = int(open(f"/proc/{pid}/stat", encoding="utf-8").read().rpartition(")")[2].split()[19])
        uptime = float(open("/proc/uptime", encoding="utf-8").read().split()[0])
    except (OSError, ValueError, IndexError):
        return None
    return (uptime - start / os.sysconf("SC_CLK_TCK")) / 60


def status(lock, folder):
    held = holders(lock)
    if held is None:
        print("holders: not read, this system lists no locks")
    for kind, pid in held or []:
        minutes = process_minutes(pid)
        print(f"holding {kind} pid {pid}, " + (f"its process {int(minutes)} min old" if minutes is not None else "its age not read"))
    for name, pid, age in marks(folder):
        repository_name = name.rpartition("-")[0]
        owner = f"{repository_name}'s pid {pid}" if repository_name and pid is not None else "an owner its name does not give"
        alive = "its process not read" if pid is None else "its process living" if lives(pid) else "its process gone"
        print(f"waiting {name}, {owner}, {int(age)} min old, {alive}")
    if not held and not marks(folder):
        print("free, no holder and no mark")
    return 0


def main():
    arguments = sys.argv[1:]
    options = {}
    while arguments and arguments[0].startswith("--") and arguments[0] != "--":
        key, _, value = arguments.pop(0)[2:].partition("=")
        options[key] = value
    lock, folder = options.pop("lock", LOCK), options.pop("waiting", WAITING)
    if options or not arguments:
        sys.stderr.write("Usage is gpu.py [--lock=<file>] [--waiting=<folder>] shared|exclusive [--minutes <n>] -- <command>, or status.\n")
        return 2
    word, rest = arguments[0], arguments[1:]
    if word == "status" and not rest:
        return status(lock, folder)
    if word not in BOUNDS:
        sys.stderr.write(f"gpu.py knows shared, exclusive and status, not {word}.\n")
        return 2
    minutes = BOUNDS[word]
    while rest and rest[0] != "--":
        option = rest.pop(0)
        if option.startswith("--minutes"):
            value = option.partition("=")[2] or (rest.pop(0) if rest else "")
            try:
                minutes = float(value)
            except ValueError:
                minutes = 0
            if minutes <= 0:
                sys.stderr.write(f"--minutes takes a number above 0, not {value or 'nothing'}.\n")
                return 2
        else:
            sys.stderr.write(f"gpu.py {word} takes --minutes, not {option}.\n")
            return 2
    # The commands, each after a --, a shared run's parts.
    parts = []
    for argument in rest:
        if argument == "--":
            parts.append([])
        else:
            parts[-1].append(argument)
    if not parts or not all(parts) or (word == "exclusive" and len(parts) > 1):
        sys.stderr.write(f"gpu.py {word} takes {'one command' if word == 'exclusive' else 'its commands'} after --.\n")
        return 2
    # A signal ends the gate as an error does, so its mark comes down and its programs are stopped.
    for number in (signal.SIGTERM, signal.SIGINT, signal.SIGHUP):
        signal.signal(number, lambda received, frame: sys.exit(128 + received))
    return exclusive(lock, folder, minutes, parts[0]) if word == "exclusive" else shared(lock, folder, minutes, parts)


sys.exit(main())
