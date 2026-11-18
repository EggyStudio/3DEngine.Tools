# Rewraps the comment lines past 100 columns of the files given with their words unchanged, each
# paragraph of comment lines that holds one filled again to the width, so N 4.11 of NORM.md holds
# without a word of a comment being written anew.
#
#   python3 -I scripts/rewrap.py [--write] <file or folder> ...
#
# Without --write it changes nothing and prints each file it would change with the count of its
# lines past the width, then the counts in all. A folder is read for every file whose language it
# knows, the folders a build or a tool fills left out. A comment line is one that opens with its
# language's mark after blanks alone, // and /// in the C family, # in Python, shell, YAML and TOML,
# and an XML comment from <!-- to --> in MSBuild and XML files; a comment after code is left as it
# is. A paragraph is the run of lines of one mark and one indent between a blank comment line, a
# line holding an XML documentation tag alone, a list's item, a line indented further and the code,
# so a list, a block of code in a comment and a summary's tags keep their lines. An XML tag is kept
# whole on a line, and a word longer than the width, an address among them, stands on a line of
# its own. A file is refused and left as it is where its lines of code differ after the rewrapping
# or its comments' words differ once their blanks are joined, so a fault of the script's changes
# no file.
import os, re, sys

sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")

COLUMNS = 100
SLASHES = re.compile(r"\.(cs|rs|slang|wgsl|glsl|hlsl)$")
HASH = re.compile(r"\.(py|sh|yml|yaml|toml)$")
XML = re.compile(r"\.(csproj|props|targets|slnx|xml)$")
FILLED = {".git", ".ref", "bin", "obj", "node_modules", "TestResults"}
# A line comment's indent, its mark and the one blank after it, then its text.
LINE = {"//": re.compile(r"^(\s*)(///?)( ?)(.*)$"), "#": re.compile(r"^(\s*)(#)( ?)(.*)$")}
# A run of tags, of characters that are no blank and of a < that opens no tag, so a tag is one
# word with the text it touches, as <see cref="Script(string, string[])"/> is, and a < b keeps
# its <.
WORD = re.compile(r"(?:<[^>]*>|<|[^\s<])+")
LONE_TAG = re.compile(r"^</?\w+[^>]*>$")
ITEM = re.compile(r"^([-*]|\d+\.) ")


def words(text):
    return WORD.findall(text)


def fill(first, rest, tokens, end=""):
    """Lines of the tokens, the first opening with first and the others with rest, each to the
    width where its words allow, and end after the last token."""
    lines, line, opened = [], first, False
    for i, token in enumerate(tokens):
        token = token + (end if i == len(tokens) - 1 else "")
        if opened and len(line) + 1 + len(token) > COLUMNS:
            lines.append(line)
            line, opened = rest, False
        line = line + (" " if opened else "") + token
        opened = True
    lines.append(line)
    return lines


def line_paragraphs(lines, mark):
    """The rewrapped lines of a file of line comments, and how many lines ran past the width."""
    pattern = LINE[mark]

    def comment(line):
        found = pattern.match(line)
        return found if found and not line.lstrip().startswith("#!") else None

    out, long, i = [], 0, 0
    while i < len(lines):
        found = comment(lines[i])
        if not found:
            out.append(lines[i])
            i += 1
            continue
        indent, slashes, text = found.group(1), found.group(2), found.group(4)
        # A blank comment line, a tag alone or a line indented further stands as it is.
        if not text.strip() or LONE_TAG.match(text.strip()) or text.startswith(" "):
            out.append(lines[i])
            i += 1
            continue
        item = ITEM.match(text)
        lead = item.group(0) if item else ""
        # A list's item runs on over the lines indented under its text, and a paragraph over the
        # lines of prose that follow it.
        j = i + 1
        while j < len(lines) and (more := comment(lines[j])) and more.group(1) == indent and more.group(2) == slashes:
            following = more.group(4)
            if item:
                if not (following.startswith(" " * len(lead)) and following[len(lead):][:1] not in ("", " ")):
                    break
            elif not following.strip() or following.startswith(" ") or LONE_TAG.match(following.strip()) or ITEM.match(following):
                break
            j += 1
        block = lines[i:j]
        over = sum(len(line) > COLUMNS for line in block)
        if over:
            long += over
            prefix = f"{indent}{slashes} "
            tokens = [w for line in block for w in words(comment(line).group(4))]
            if item:
                tokens = tokens[1:]
            out += fill(prefix + lead, prefix + " " * len(lead), tokens)
        else:
            out += block
        i = j
    return out, long


def xml_comments(lines):
    """The rewrapped lines of an MSBuild or XML file, and how many comment lines ran past the width."""
    out, long, i = [], 0, 0
    while i < len(lines):
        opened = re.match(r"^(\s*)<!--(.*)$", lines[i])
        if not opened or "-->" in lines[i] and len(lines[i]) <= COLUMNS:
            out.append(lines[i]); i += 1; continue
        j = i
        while "-->" not in lines[j] and j + 1 < len(lines):
            j += 1
        block = lines[i:j + 1]
        over = sum(len(line) > COLUMNS for line in block)
        after = block[-1].split("-->", 1)[1]
        if not over or after.strip():
            out += block
        else:
            long += over
            indent = opened.group(1)
            text = " ".join(block).split("<!--", 1)[1].rsplit("-->", 1)[0]
            out += fill(f"{indent}<!-- ", f"{indent}     ", words(text), end=" -->")
        i = j + 1
    return out, long


def comment_text(lines, path):
    """Every comment's text in its order with its blanks joined, and every other line, the two the
    rewrapping keeps. The text is split at blanks alone, apart from the words the rewrapping fills
    with, so a word the filling lost or split is read as a difference."""
    said, code, in_xml = [], [], False
    for line in lines:
        if XML.search(path):
            if in_xml or line.lstrip().startswith("<!--"):
                said += line.replace("<!--", " ").replace("-->", " ").split()
                in_xml = "-->" not in line
                continue
        else:
            found = LINE["//" if SLASHES.search(path) else "#"].match(line)
            if found and not line.lstrip().startswith("#!"):
                said += found.group(4).split()
                continue
        code.append(line)
    return " ".join(said), code


def rewrap(path):
    """The file's text rewrapped, the count of its lines past the width that were rewrapped and of
    those left as they stand, and why it is refused, or None where it is not."""
    with open(path, encoding="utf-8", newline="") as f:
        text = f.read()
    end = "\r\n" if "\r\n" in text else "\n"
    lines = text.split(end)
    if XML.search(path):
        out, long = xml_comments(lines)
    else:
        out, long = line_paragraphs(lines, "//" if SLASHES.search(path) else "#")
    (said, code), (said_after, code_after) = comment_text(lines, path), comment_text(out, path)
    left = sum(len(line) > COLUMNS for line in out) - sum(len(line) > COLUMNS for line in code_after)
    if code != code_after:
        return text, long, left, "its lines of code would differ"
    if said != said_after:
        return text, long, left, "its comments' words would differ"
    return end.join(out), long, left, None


def files(arguments):
    for argument in arguments:
        if os.path.isdir(argument):
            for folder, children, names in os.walk(argument):
                children[:] = sorted(c for c in children if c not in FILLED)
                for name in sorted(names):
                    if SLASHES.search(name) or HASH.search(name) or XML.search(name):
                        yield os.path.join(folder, name)
        else:
            yield argument


def main():
    write = "--write" in sys.argv[1:]
    arguments = [a for a in sys.argv[1:] if a != "--write"]
    if not arguments:
        sys.exit("Usage is rewrap.py [--write] <file or folder> ....")
    changed = lines = standing = refused = 0
    for path in files(arguments):
        text, long, left, refusal = rewrap(path)
        standing += left
        if refusal:
            refused += 1
            print(f"{path} refused, since {refusal}")
            continue
        with open(path, encoding="utf-8", newline="") as f:
            old = f.read()
        if text == old:
            continue
        changed += 1
        lines += long
        print(f"{path} {long} lines past {COLUMNS} columns")
        if write:
            with open(path, "w", encoding="utf-8", newline="") as f:
                f.write(text)
    verb = "rewrapped" if write else "would be rewrapped"
    print(f"{lines} comment lines past {COLUMNS} columns in {changed} files {verb}, {standing} left as they stand, {refused} files refused")
    sys.exit(1 if refused else 0)


main()
