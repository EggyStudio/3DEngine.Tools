# Sweeps the repositories given by scripts/sweep.py's rules and writes one Markdown report of them
# to the path given, the counts of each repository's rules and top folders, the ten files a rule
# holds most of, and, where a previous report is given, the change in each count since it. It
# reads the repositories and writes the report alone.
#
#   python3 -I scripts/daily.py <report.md> [--previous=<report.md>] <repo> ...
#
# The names come from FORBIDDEN_NAMES alone, as the sweep has them, and a place or a file's path
# holding one is written with it put out of sight. The report ends with its counts as JSON inside
# an HTML comment, which a later report reads its change from, so the Markdown above it is free to
# change its form. The exit code is 0 where the report is written, whatever it holds.
import collections, datetime, json, os, re, sys, time

sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")

# The sweep's reading, imported from beside this script with no compiled copy written there.
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sweep import forbidden_names, read_tree  # noqa: E402

WORST = 10
COUNTS = re.compile(r"<!-- daily counts (\{.*\}) -->", re.S)


def top_folder(path):
    return path.split("/", 1)[0] if "/" in path else "(the root)"


def change(now, before):
    if before is None:
        return "new"
    return "0" if now == before else f"{now - before:+d}"


def read_previous(path):
    """The counts a previous report carries, by repository, rule and level, or None."""
    if not path:
        return None
    if not os.path.exists(path):
        sys.exit(f"{path}, the previous report, does not exist.")
    found = COUNTS.search(open(path, encoding="utf-8").read())
    if not found:
        sys.exit(f"{path} carries no counts, so it is no report this script wrote.")
    return json.loads(found.group(1))


def report(repos, previous, forbidden):
    lines, counts = [f"# Sweep of {datetime.date.today().isoformat()}", ""], {}
    if not forbidden:
        lines += ["FORBIDDEN_NAMES was not set, so the names were not read for.", ""]
    summary = ["| repository | FAIL | WARN | FAIL since | WARN since | time |", "|---|---|---|---|---|---|"]
    sections = []
    for repo in repos:
        name = os.path.basename(os.path.abspath(repo))
        started = time.monotonic()
        found, texts, pictures, tracked = read_tree(repo, forbidden)
        took = time.monotonic() - started
        by_rule = collections.Counter((level, rule) for level, rule, _, _ in found)
        counts[name] = {f"{level} {rule}": count for (level, rule), count in by_rule.items()}
        before = None if previous is None else previous.get(name, {})
        totals = {level: sum(c for (l, _), c in by_rule.items() if l == level) for level in ("FAIL", "WARN")}
        was = {level: None if before is None else sum(c for key, c in before.items() if key.startswith(level + " "))
               for level in ("FAIL", "WARN")}
        summary.append(f"| {name} | {totals['FAIL']} | {totals['WARN']} | {change(totals['FAIL'], was['FAIL']) if before is not None else ''}"
                       f" | {change(totals['WARN'], was['WARN']) if before is not None else ''} | {took:.1f} s |")

        section = [f"## {name}", "", f"{texts} text files and {pictures} pictures of {tracked} tracked, read in {took:.1f} s.", ""]
        section += ["| level | rule | count | since |", "|---|---|---|---|"]
        for (level, rule), count in sorted(by_rule.items(), key=lambda item: (item[0][0] != "FAIL", -item[1], item[0][1])):
            key = f"{level} {rule}"
            section.append(f"| {level} | {rule} | {count} | {change(count, before.get(key)) if before is not None else ''} |")
        for key, count in sorted((before or {}).items()):
            if key not in counts[name] and count:
                level, rule = key.split(" ", 1)
                section.append(f"| {level} | {rule} | 0 | {-count:+d} |")
        if not by_rule and not before:
            section.append("| | no rule broken | 0 | |")
        section.append("")

        folders = collections.defaultdict(collections.Counter)
        for level, _, path, _ in found:
            folders[top_folder(path)][level] += 1
        if folders:
            section += ["| top folder | FAIL | WARN |", "|---|---|---|"]
            for folder, levels in sorted(folders.items(), key=lambda item: (-sum(item[1].values()), item[0])):
                section.append(f"| `{folder}` | {levels['FAIL']} | {levels['WARN']} |")
            section.append("")

        for (level, rule), _ in sorted(by_rule.items(), key=lambda item: (item[0][0] != "FAIL", -item[1], item[0][1])):
            files = collections.Counter(path for l, r, path, _ in found if (l, r) == (level, rule))
            section += [f"### {level} {rule}", "", "| file | count |", "|---|---|"]
            section += [f"| `{path}` | {count} |" for path, count in sorted(files.items(), key=lambda item: (-item[1], item[0]))[:WORST]]
            if len(files) > WORST:
                section.append(f"| {len(files) - WORST} files more | {sum(sorted(files.values(), reverse=True)[WORST:])} |")
            section.append("")
        sections += section

    lines += summary + [""] + sections
    lines.append(f"<!-- daily counts {json.dumps(counts, sort_keys=True)} -->")
    return "\n".join(lines) + "\n", counts


def main():
    arguments = [a for a in sys.argv[1:] if not a.startswith("--previous=")]
    previous_path = next((a.split("=", 1)[1] for a in sys.argv[1:] if a.startswith("--previous=")), None)
    if len(arguments) < 2:
        sys.exit("Usage is daily.py <report.md> [--previous=<report.md>] <repo> ....")
    out, repos = arguments[0], arguments[1:]
    previous = read_previous(previous_path)
    text, counts = report(repos, previous, forbidden_names())
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    totals = ", ".join(f"{name} {sum(c.values())}" for name, c in counts.items())
    print(f"wrote {out}, {totals}")


main()
