# Sums a day's token use by session from the transcripts' usage fields, so the hourly watch can say
# whether the quiet protocol works, reading nothing of what the sessions said.
#
#   python3 -I scripts/usage.py [<date>] [--since=<hour>] [--projects=<folder>] [--cache=<folder>]
#
# It reads every transcript under the projects folder, ~/.claude/projects where none is given, its
# subagents' among them, and of each line only an assistant entry's usage, time, session and
# request, and an agent-name entry's name. A request is counted once however many entries repeat
# its usage, and its context is its input, the cache written and the cache read. It prints a line a
# session, the largest first, with its turns, cache read, cache written, output and mean and largest
# context, the turns and cache read of each hour on a line of its own, and a total, on the date
# given in local time, today's where none is, from the hour --since gives where a watch reads the
# last hours alone. A line it cannot read is counted and its file named. The exit code is 0, or 2
# where the projects folder does not stand.
#
# The transcripts of a month hold gigabytes, so what each holds is kept in a cache of SQLite, in a
# folder the cache option gives or the user's cache folder, never in a repository, one file for a
# projects folder. A transcript is keyed by its path, and holds its size, its modified time, a
# check of its first bytes and the byte offset up to which its lines were read, so a run streams
# only what was appended since, a line at a time and never a file whole, and answers the day from
# the rows. A transcript that shrank, or changed where it was read, is read again whole. A last line
# not yet ended by a line feed is left for the run after it, since it may be mid-write. The
# cache's reading is said on the error output, its bytes read and its bytes in all.
import collections, datetime, glob, hashlib, json, os, sqlite3, sys

sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")

# How much of a transcript's start is kept as its check, so a file written over is read again.
HEAD_BYTES = 4096


def cache_folder():
    if os.name == "nt":
        base = os.environ.get("LOCALAPPDATA") or os.path.join(os.path.expanduser("~"), "AppData", "Local")
    else:
        base = os.environ.get("XDG_CACHE_HOME") or os.path.join(os.path.expanduser("~"), ".cache")
    return os.path.join(base, "3DEngine.Tools")


def open_cache(folder, projects):
    os.makedirs(folder, exist_ok=True)
    name = "usage-" + hashlib.sha1(os.path.abspath(projects).encode("utf-8")).hexdigest()[:12] + ".sqlite"
    db = sqlite3.connect(os.path.join(folder, name), timeout=60)
    db.executescript("""
        create table if not exists files (path text primary key, size integer, mtime real, head text, offset integer, unread integer);
        create table if not exists usage (path text, pos integer, key text, session text, at real,
            input integer, written integer, read integer, output integer);
        create index if not exists usage_at on usage (at);
        create index if not exists usage_path on usage (path);
        create table if not exists names (path text, pos integer, session text, name text);
        create index if not exists names_path on names (path);
    """)
    return db


def head_of(path, length):
    """A check of a transcript's first bytes, as many as were read of it, up to HEAD_BYTES."""
    with open(path, "rb") as f:
        return hashlib.sha1(f.read(min(length, HEAD_BYTES))).hexdigest()


def index(db, path, size, mtime):
    """Brings one transcript's rows up to date, reading from its kept offset, and returns the bytes
    it read. A transcript is read again whole where it shrank, or where its start changed."""
    row = db.execute("select size, mtime, head, offset, unread from files where path = ?", (path,)).fetchone()
    offset, unread = 0, 0
    if row and row[0] <= size and row[2] == head_of(path, row[3]) and (row[0] != size or row[1] == mtime):
        if row[0] == size:
            return 0
        offset, unread = row[3], row[4]
    elif row:
        for table in ("usage", "names"):
            db.execute(f"delete from {table} where path = ?", (path,))
    read_bytes = 0
    usage_rows, name_rows = [], []
    with open(path, "rb") as f:
        f.seek(offset)
        # Binary lines stream a line at a time, and one not ended by a line feed is not taken.
        for raw in f:
            if not raw.endswith(b"\n"):
                break
            pos, offset = offset, offset + len(raw)
            read_bytes += len(raw)
            try:
                entry = json.loads(raw.decode("utf-8", "replace"))
            except ValueError:
                unread += 1
                continue
            if not isinstance(entry, dict):
                continue
            if entry.get("type") == "agent-name" and entry.get("agentName"):
                name_rows.append((path, pos, str(entry.get("sessionId")), entry["agentName"]))
            message = entry.get("message")
            usage = message.get("usage") if entry.get("type") == "assistant" and isinstance(message, dict) else None
            if not usage:
                continue
            # The time is UTC in the transcript, kept as seconds since the epoch.
            try:
                at = datetime.datetime.fromisoformat(str(entry.get("timestamp")).replace("Z", "+00:00")).timestamp()
            except ValueError:
                unread += 1
                continue
            # Entries of one response repeat its usage, so the request is its key.
            key = entry.get("requestId") or f"{path}:{entry.get('uuid')}"
            usage_rows.append((path, pos, key, str(entry.get("sessionId")), at, usage.get("input_tokens", 0),
                               usage.get("cache_creation_input_tokens", 0), usage.get("cache_read_input_tokens", 0),
                               usage.get("output_tokens", 0)))
    db.executemany("insert into usage values (?,?,?,?,?,?,?,?,?)", usage_rows)
    db.executemany("insert into names values (?,?,?,?)", name_rows)
    db.execute("insert or replace into files values (?,?,?,?,?,?)", (path, size, mtime, head_of(path, offset), offset, unread))
    db.commit()
    return read_bytes


def main():
    arguments = [a for a in sys.argv[1:] if not a.startswith("--")]
    option = lambda name: next((a.split("=", 1)[1] for a in sys.argv[1:] if a.startswith(f"--{name}=")), None)
    projects = option("projects") or os.path.join(os.path.expanduser("~"), ".claude", "projects")
    day = arguments[0] if arguments else datetime.datetime.now().strftime("%Y-%m-%d")
    since = int(option("since") or 0)
    if not os.path.isdir(projects):
        print(f"{projects} does not stand, so no transcript is read.", file=sys.stderr)
        sys.exit(2)

    projects = os.path.abspath(projects)
    db = open_cache(option("cache") or cache_folder(), projects)
    paths = sorted(glob.glob(os.path.join(projects, "**", "*.jsonl"), recursive=True))
    present, read_bytes, total_bytes = set(paths), 0, 0
    for path in paths:
        status = os.stat(path)
        total_bytes += status.st_size
        read_bytes += index(db, path, status.st_size, status.st_mtime)
    for (path,) in db.execute("select path from files").fetchall():
        if path not in present:
            for table in ("usage", "names", "files"):
                db.execute(f"delete from {table} where path = ?", (path,))
    db.commit()

    # The day in local time is a range of seconds, so the rows are chosen by the cache's index.
    start = datetime.datetime.strptime(day, "%Y-%m-%d").astimezone()
    end = (datetime.datetime.strptime(day, "%Y-%m-%d") + datetime.timedelta(days=1)).astimezone()
    names = {}
    for path, session, name in db.execute("select path, session, name from names order by path, pos"):
        if path in present:
            names[session] = name
    requests = {}
    rows = db.execute("select path, pos, key, session, at, input, written, read, output from usage where at >= ? and at < ?",
                      (start.timestamp(), end.timestamp())).fetchall()
    for path, pos, key, session, at, input_tokens, written, read, output in sorted(rows, key=lambda row: (row[0], row[1])):
        if path not in present:
            continue
        hour = datetime.datetime.fromtimestamp(at).hour
        if hour < since:
            continue
        requests[key] = (session, f"{hour:02}", {"input_tokens": input_tokens, "cache_creation_input_tokens": written,
                                                 "cache_read_input_tokens": read, "output_tokens": output})
    unread = {path: count for path, count in db.execute("select path, unread from files") if path in present and count}
    db.close()

    sessions = collections.defaultdict(list)
    hours = collections.defaultdict(lambda: [0, 0])
    for session, hour, usage in requests.values():
        sessions[session].append(usage)
        hours[hour][0] += 1
        hours[hour][1] += usage.get("cache_read_input_tokens", 0)

    def context(usage):
        return sum(usage.get(k, 0) for k in ("input_tokens", "cache_creation_input_tokens", "cache_read_input_tokens"))

    def sums(usages):
        return [sum(u.get(k, 0) for u in usages) for k in ("cache_read_input_tokens", "cache_creation_input_tokens", "output_tokens")]

    for session, usages in sorted(sessions.items(), key=lambda item: -sum(context(u) + u.get("output_tokens", 0) for u in item[1])):
        read, written, output = sums(usages)
        contexts = [context(u) for u in usages]
        print(f"{names.get(session) or str(session)[:8]}  turns {len(usages)}  cache read {read:,}  cache written {written:,}  output {output:,}"
              f"  context mean {round(sum(contexts) / len(contexts)):,}  largest {max(contexts):,}")
    print("by hour, local time")
    for hour, (turns, read) in sorted(hours.items()):
        print(f"  {hour}h  {turns} turns  {read:,} read")
    read, written, output = sums([u for usages in sessions.values() for u in usages])
    print(f"all  turns {len(requests)}  cache read {read:,}  cache written {written:,}  output {output:,}")
    for path, count in sorted(unread.items()):
        print(f"{count} line{'' if count == 1 else 's'} not read in {path}")
    print(f"cache: {read_bytes:,} bytes read of {total_bytes:,} in {len(paths)} transcripts", file=sys.stderr)


main()
