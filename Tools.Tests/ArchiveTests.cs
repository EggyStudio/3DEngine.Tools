using System.Text;
using Xunit;

namespace Tools.Tests;

public class ArchiveTests
{
    private const string Head = "# Review\n\nThe head.\n\n## Now\n\n1. An item.\n\n## Replies\n\n";

    // A reply of some 3 KB, its number in every line, so a paragraph is known wherever it lands.
    private static string Reply(int n) =>
        string.Join("\n", Enumerable.Range(0, 30).Select(line => $"Reply {n} line {line}, " + new string('a', 80))) + "\n";

    // A reply is its first paragraph and the indented, listed, tabled or fenced ones after it, so
    // the second reply's list, table and indented lines and the last's fenced block, a blank line
    // inside it, go with them.
    private static string[] Replies() =>
    [
        Reply(1),
        Reply(2) + "\n- a listed point\n- another\n\n| a | b |\n|---|---|\n| 1 | 2 |\n\n    an indented line\n",
        .. Enumerable.Range(3, 27).Select(Reply),
        "Reply 30 with a block.\n\n```bash\nfirst\n\nsecond\n```\n",
    ];

    [NeedsPythonFact]
    public void The_Oldest_Replies_Move_Whole_Until_The_Review_Stands_Under_40_KB_And_A_Second_Run_Moves_None()
    {
        using var folder = new Folder();
        var replies = Replies();
        folder.Write(".github/REVIEW.md", Head + string.Join("\n", replies));

        var (code, printed) = Programs.Attempt("archive.py", [], folder.Path);
        var review = folder.Read(".github/REVIEW.md");
        var archive = folder.Read(".github/REVIEW-ARCHIVE.md");

        Assert.True(code == 0, printed);
        Assert.True(Encoding.UTF8.GetByteCount(review) < 40 * 1024, $"the review is {Encoding.UTF8.GetByteCount(review)} bytes");
        Assert.StartsWith(Head, review);
        Assert.StartsWith("# Review archive\n", archive);
        // Every reply stands whole in one file or the other, the oldest in the archive, in order.
        var moved = replies.TakeWhile(reply => archive.Contains(reply, StringComparison.Ordinal)).Count();
        Assert.True(moved > 0, "no reply moved");
        Assert.All(replies[moved..], reply => Assert.Contains(reply, review));
        Assert.All(replies[..moved], reply => Assert.DoesNotContain(reply.Split('\n')[0] + "\n", review));
        Assert.Contains($"{moved} replies moved to .github/REVIEW-ARCHIVE.md", printed);
        Assert.Contains(replies[^1], review);

        var (again, said) = Programs.Attempt("archive.py", [], folder.Path);
        Assert.True(again == 0, said);
        Assert.Contains("under 40 KB, so nothing moved", said);
        Assert.Equal(review, folder.Read(".github/REVIEW.md"));
    }

    [NeedsPythonFact]
    public void A_Limit_Falling_Inside_A_Reply_Moves_The_Reply_Whole_Its_List_Table_And_Indented_Lines_With_It()
    {
        using var folder = new Folder();
        var replies = Replies();
        var text = Head + string.Join("\n", replies);
        folder.Write(".github/REVIEW.md", text);
        // A limit under the review less its first reply, and over it less its first reply and its
        // second's first paragraph, so a move by paragraphs would stop inside the second reply.
        var limit = (Encoding.UTF8.GetByteCount(text) - Encoding.UTF8.GetByteCount(replies[0] + "\n") - 1) / 1024;

        var (code, printed) = Programs.Attempt("archive.py", [], folder.Path, $"--under={limit}");
        var review = folder.Read(".github/REVIEW.md");
        var archive = folder.Read(".github/REVIEW-ARCHIVE.md");

        Assert.True(code == 0, printed);
        Assert.Contains("2 replies moved", printed);
        Assert.Contains(replies[1], archive);
        Assert.DoesNotContain("a listed point", review);
        Assert.StartsWith(Head + "Reply 3 line 0, ", review);
    }

    [NeedsPythonFact]
    public void Replies_Moved_Follow_Those_The_Archive_Holds_And_A_Review_With_No_Replies_Is_Refused()
    {
        using var folder = new Folder();
        folder.Write(".github/REVIEW.md", Head + string.Join("\n", Replies()));
        folder.Write(".github/REVIEW-ARCHIVE.md", "# Review archive\n\nThe head.\n\n## Replies archived\n\nAn older reply.\n");

        var (code, printed) = Programs.Attempt("archive.py", [], folder.Path);
        var archive = folder.Read(".github/REVIEW-ARCHIVE.md");

        Assert.True(code == 0, printed);
        Assert.StartsWith("# Review archive\n\nThe head.\n\n## Replies archived\n\nAn older reply.\n\nReply 1 line 0, ", archive);

        folder.Write(".github/REVIEW.md", "# Review\n\n## Now\n\n1. An item.\n");
        var (refused, said) = Programs.Attempt("archive.py", [], folder.Path);
        Assert.Equal(2, refused);
        Assert.Contains("holds no ## Replies section", said);
    }

    // A folder standing for a repository's checkout, deleted with it.
    private sealed class Folder : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("archive-").FullName;

        public void Write(string relative, string text)
        {
            var file = System.IO.Path.Combine(Path, relative);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file)!);
            File.WriteAllText(file, text);
        }

        public string Read(string relative) => File.ReadAllText(System.IO.Path.Combine(Path, relative)).ReplaceLineEndings("\n");

        public void Dispose() => Directory.Delete(Path, true);
    }
}
