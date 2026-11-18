# Sums a day's token use by session from the transcripts' usage fields, so the hourly watch can say
# whether the quiet protocol works, reading nothing of what the sessions said.
#
#   python3 -I scripts/usage.py [<date>] [--projects=<folder>]
#
# It reads every transcript under the projects folder, ~/.claude/projects where none is given, its
# subagents' among them, and of each line only an assistant entry's usage, time, session and
# request, and an agent-name entry's name. A request is counted once however many entries repeat
# its usage, and its context is its input, the cache written and the cache read. It prints a line a
# session, the largest first, with its turns, cache read, cache written, output and mean and largest
# context, a line of turns and cache read by hour, and a total, on the date given in UTC, today's
# where none is. A line it cannot read is counted and its file named. The exit code is 0, or 2
# where the projects folder does not stand.
import collections, datetime, glob, json, os, sys

sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")


def main():
    arguments = [a for a in sys.argv[1:] if not a.startswith("--")]
    projects = next((a.split("=", 1)[1] for a in sys.argv[1:] if a.startswith("--projects=")),
                    os.path.join(os.path.expanduser("~"), ".claude", "projects"))
    day = arguments[0] if arguments else datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%d")
    if not os.path.isdir(projects):
        print(f"{projects} does not stand, so no transcript is read.", file=sys.stderr)
        sys.exit(2)

    requests, names, unread = {}, {}, collections.Counter()
    for path in sorted(glob.glob(os.path.join(projects, "**", "*.jsonl"), recursive=True)):
        with open(path, encoding="utf-8", errors="replace") as f:
            for line in f:
                try:
                    entry = json.loads(line)
                except ValueError:
                    unread[path] += 1
                    continue
                if not isinstance(entry, dict):
                    continue
                if entry.get("type") == "agent-name" and entry.get("agentName"):
                    names[entry.get("sessionId")] = entry["agentName"]
                message = entry.get("message")
                usage = message.get("usage") if entry.get("type") == "assistant" and isinstance(message, dict) else None
                if not usage or not str(entry.get("timestamp", "")).startswith(day):
                    continue
                # Entries of one response repeat its usage, so the request is its key.
                key = entry.get("requestId") or f"{path}:{entry.get('uuid')}"
                requests[key] = (entry.get("sessionId"), entry["timestamp"][11:13], usage)

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
    print("by hour  " + "  ".join(f"{hour}h {turns} turns {read:,} read" for hour, (turns, read) in sorted(hours.items())))
    read, written, output = sums([u for usages in sessions.values() for u in usages])
    print(f"all  turns {len(requests)}  cache read {read:,}  cache written {written:,}  output {output:,}")
    for path, count in sorted(unread.items()):
        print(f"{count} line{'' if count == 1 else 's'} not read in {path}")


main()
