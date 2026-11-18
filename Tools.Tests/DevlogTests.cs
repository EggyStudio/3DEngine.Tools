using Xunit;

namespace Tools.Tests;

public class DevlogTests
{
    [NeedsPythonFact]
    public void A_Done_Item_Of_The_Review_Is_Listed_On_The_Day_Of_Its_Commit()
    {
        var repo = Directory.CreateTempSubdirectory("devlog-").FullName;
        try
        {
            // A repository of one commit, which the review's one item names as the commit that
            // closed it.
            Git(repo, "init", "-q");
            File.WriteAllText(Path.Combine(repo, "README.md"), "# Sample\n");
            Git(repo, "add", "-A");
            Git(repo, "commit", "-q", "-m", "The first commit.");
            var hash = Git(repo, "rev-parse", "--short=7", "HEAD").Trim();
            var date = Git(repo, "log", "-1", "--format=%cs").Trim();
            Directory.CreateDirectory(Path.Combine(repo, ".github"));
            File.WriteAllText(Path.Combine(repo, ".github", "REVIEW.md"),
                $"# Review\n\n## Now\n\n1. **The first item.** Done at `{hash}`.\n\n## Verdicts\n\nNone yet.\n");

            Programs.Script("devlog.py", repo, "Sample");

            var devlog = File.ReadAllLines(Path.Combine(repo, "DEVLOG.md"));
            Assert.Equal("# Sample devlog", devlog[0]);
            var day = Array.IndexOf(devlog, $"## {date}");
            Assert.True(day > 0, $"no heading for {date}");
            Assert.Equal("Done that day", devlog[day + 2]);
            Assert.Equal($"- Item 1 at `{hash}`. The first item.", devlog[day + 4]);
        }
        finally
        {
            // Git writes its objects read-only, which Windows will not delete as they are.
            foreach (var file in Directory.EnumerateFiles(repo, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(repo, true);
        }
    }

    [NeedsPythonFact]
    public void With_Commits_Each_Day_Lists_Its_Commits_Sentences_Newest_First_The_Reviews_And_The_Devlogs_Own_Left_Out()
    {
        var repo = Directory.CreateTempSubdirectory("devlog-").FullName;
        try
        {
            Git(repo, "init", "-q");
            Commit(repo, "a.txt", "The first file holds a letter.");
            Commit(repo, ".github/REVIEW.md", "The review reads the first file.");
            Commit(repo, "b.txt", "The second file holds a letter.\nIts letter is the first's.");
            Commit(repo, "DEVLOG.md", "The devlog is written again.");
            var (first, second) = (Git(repo, "rev-parse", "--short=7", "HEAD~3").Trim(), Git(repo, "rev-parse", "--short=7", "HEAD~1").Trim());

            Programs.Script("devlog.py", repo, "Sample", "--commits");
            var devlog = File.ReadAllLines(Path.Combine(repo, "DEVLOG.md"));
            var commits = devlog.SkipWhile(line => line != "Commits that day").Skip(2).TakeWhile(line => line.Length > 0).ToList();

            Assert.Equal([$"- `{second}` The second file holds a letter. Its letter is the first's.", $"- `{first}` The first file holds a letter."], commits);

            Programs.Script("devlog.py", repo, "Sample");
            Assert.DoesNotContain("Commits that day", File.ReadAllLines(Path.Combine(repo, "DEVLOG.md")));
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(repo, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(repo, true);
        }
    }

    [NeedsPythonFact]
    public void A_Commit_Whose_Sentence_Names_An_Engine_Is_Left_Out_And_Counted_On_The_Output_Alone()
    {
        var repo = Directory.CreateTempSubdirectory("devlog-").FullName;
        try
        {
            Git(repo, "init", "-q");
            Commit(repo, "a.txt", "The assembly is named 3DEngine.Sample.");
            Commit(repo, "b.txt", "It follows the mend Bevy" + "CSharp made.");
            var kept = Git(repo, "rev-parse", "--short=7", "HEAD~1").Trim();

            var printed = Programs.Script("devlog.py", repo, "Sample", "--commits");
            var devlog = File.ReadAllText(Path.Combine(repo, "DEVLOG.md"));

            Assert.Contains($"- `{kept}` The assembly is named 3DEngine.Sample.", devlog);
            Assert.DoesNotContain("follows the mend", devlog);
            Assert.DoesNotContain("left out", devlog);
            Assert.Contains("1 commit left out, its sentence naming an engine", printed);
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(repo, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(repo, true);
        }
    }

    [NeedsPythonFact]
    public void A_Commit_Whose_Sentence_Would_Fail_The_Line_Check_Or_Names_The_Bridge_Is_Left_Out_And_Counted()
    {
        var repo = Directory.CreateTempSubdirectory("devlog-").FullName;
        try
        {
            Git(repo, "init", "-q");
            Commit(repo, "a.txt", "The page is kept.");
            // The two-word form and the middle dot are joined from parts, which no line here holds.
            Commit(repo, "b.txt", "A cheat" + " sheet lists the types.");
            Commit(repo, "c.txt", "Two steps \u00b7 one pass.");
            Commit(repo, "d.txt", "It follows the bridge's mend.");
            var kept = Git(repo, "rev-parse", "--short=7", "HEAD~3").Trim();

            var printed = Programs.Script("devlog.py", repo, "Sample", "--commits");
            var commits = File.ReadAllLines(Path.Combine(repo, "DEVLOG.md")).SkipWhile(line => line != "Commits that day").Skip(2).TakeWhile(line => line.Length > 0).ToList();

            Assert.Equal([$"- `{kept}` The page is kept."], commits);
            Assert.Contains("1 commit left out, its sentence naming an engine", printed);
            Assert.Contains("2 commits left out, their sentences failing the line check", printed);
            Assert.DoesNotContain("left out", File.ReadAllText(Path.Combine(repo, "DEVLOG.md")));
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(repo, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(repo, true);
        }
    }

    // A commit of one file, its message the three marks, a blank line and the lines given.
    private static void Commit(string repo, string path, string lines)
    {
        var file = Path.Combine(repo, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, path + "\n");
        Git(repo, "add", "-A");
        File.WriteAllText(Path.Combine(repo, ".git", "message"), "\u200e \u200e \u200e\n\n" + lines + "\n");
        Git(repo, "commit", "-q", "--cleanup=verbatim", "-F", Path.Combine(repo, ".git", "message"));
    }

    [NeedsPythonFact]
    public void Run_With_No_Repository_It_Says_How_It_Is_Run()
    {
        var (code, printed) = Programs.Attempt("devlog.py", []);

        Assert.True(code != 0, printed);
        Assert.Contains("Usage is devlog.py <repo path> <Name> [sent-pictures.json] [--fixes <fixes.json>] [--commits].", printed);
        Assert.DoesNotContain("Traceback", printed);
    }

    // The machine's own settings left out, an identity given and signing off, so a commit is made
    // alike in a workflow with no identity and on a desktop that signs.
    private static string Git(string repo, params string[] arguments) =>
        Programs.Run("git", repo, ["-c", "user.name=Tools", "-c", "user.email=tools@example.com", "-c", "commit.gpgsign=false", .. arguments]);
}
