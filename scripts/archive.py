# Moves a review's oldest replies whole from .github/REVIEW.md into .github/REVIEW-ARCHIVE.md until
# the review stands under 40 KB, as the review moved them by hand, so a review that grows past what
# status.py says keeps its direction at hand.
#
#   python3 -I scripts/archive.py <repo> [--under=<KB>]
#
# A reply is its first paragraph and the indented, listed, tabled or fenced paragraphs after it,
# moved as one and never split, the oldest first, and nothing above the review's "## Replies" is
# touched. The archive is made where the repository has none, and the replies moved follow those it
# holds, so it reads oldest first. It commits nothing. The exit code is 0, or 2 where the review or
# its Replies section is missing.
import os, re, sys

sys.stdout.reconfigure(encoding="utf-8"); sys.stderr.reconfigure(encoding="utf-8")

ARCHIVE_HEAD = ("# Review archive\n\nThe replies a review no longer needs at hand, oldest first, moved here whole so `REVIEW.md` stays\n"
                "under its size.\n\n## Replies archived\n")
# A paragraph that goes with the one before it: an indented, listed, tabled or fenced one.
FOLLOWS = re.compile(r"^(\s+\S|[-*+] |\d+\. |\||```)")


def stop(sentence):
    print(sentence, file=sys.stderr)
    sys.exit(2)


def paragraphs(text):
    """The text's paragraphs, a fenced block's blank lines kept inside its paragraph."""
    found, current, fenced = [], [], False
    for line in text.split("\n"):
        if line.startswith("```"):
            fenced = not fenced
        if not line.strip() and not fenced:
            if current:
                found.append("\n".join(current))
                current = []
            continue
        current.append(line)
    if current:
        found.append("\n".join(current))
    return found


def replies(text):
    """The paragraphs gathered into replies, each its first paragraph and those that follow it."""
    gathered = []
    for paragraph in paragraphs(text):
        if gathered and FOLLOWS.match(paragraph):
            gathered[-1].append(paragraph)
        else:
            gathered.append([paragraph])
    return ["\n\n".join(reply) for reply in gathered]


def main():
    arguments = [a for a in sys.argv[1:] if not a.startswith("--")]
    under = next((int(a.split("=", 1)[1]) for a in sys.argv[1:] if a.startswith("--under=")), 40) * 1024
    if not arguments:
        sys.exit("Usage is archive.py <repo> [--under=<KB>].")
    github = os.path.join(arguments[0], ".github")
    review_path, archive_path = os.path.join(github, "REVIEW.md"), os.path.join(github, "REVIEW-ARCHIVE.md")
    if not os.path.exists(review_path):
        stop(f"{review_path} does not stand, so there is no review to move replies from.")
    with open(review_path, encoding="utf-8", newline="") as f:
        review = f.read()
    ending = "\r\n" if "\r\n" in review else "\n"
    review = review.replace("\r\n", "\n")
    found = re.search(r"^## Replies[ \t]*\n", review, re.M)
    if not found:
        stop(f"{review_path} holds no ## Replies section, so no reply is moved.")
    # The Replies section runs to the next heading of its rank, or to the end.
    after = re.search(r"^## ", review[found.end():], re.M)
    end = found.end() + after.start() if after else len(review)
    head, section, tail = review[:found.end()], review[found.end():end], review[end:]
    size = len(review.encode("utf-8"))
    if size < under:
        print(f"{review_path} is {size // 1024} KB, under {under // 1024} KB, so nothing moved")
        return

    kept = replies(section)
    moved = []
    while kept and size >= under:
        reply = kept.pop(0)
        moved.append(reply)
        size -= len((reply + "\n\n").encode("utf-8"))
    section = "\n" + "\n\n".join(kept) + "\n" + ("\n" if tail else "") if kept else "\n"
    if os.path.exists(archive_path):
        with open(archive_path, encoding="utf-8", newline="") as f:
            archive = f.read().replace("\r\n", "\n").rstrip("\n") + "\n"
    else:
        archive = ARCHIVE_HEAD
    archive += "\n" + "\n\n".join(moved) + "\n"
    with open(review_path, "w", encoding="utf-8", newline="") as f:
        f.write((head + section + tail).replace("\n", ending))
    with open(archive_path, "w", encoding="utf-8", newline="") as f:
        f.write(archive.replace("\n", ending))
    print(f"{len(moved)} replies moved to .github/REVIEW-ARCHIVE.md, REVIEW.md {os.path.getsize(review_path) // 1024} KB "
          f"and the archive {os.path.getsize(archive_path) // 1024} KB")
    if kept == [] and os.path.getsize(review_path) >= under:
        print(f"every reply moved and REVIEW.md still stands at {os.path.getsize(review_path) // 1024} KB, its head the rest")


main()
