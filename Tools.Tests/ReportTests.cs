using Xunit;

namespace Tools.Tests;

public class ReportTests
{
    [NeedsPythonFact]
    public void A_Commits_Block_Gives_What_The_Script_Read_Its_Tests_Read_From_The_Trx_Given()
    {
        using var world = new World();
        world.Commit("a.txt", "a\n", "The first file.");
        world.Write("Sample.Tests/ATests.cs", "class ATests\n{\n    [Fact]\n    public void A() { }\n\n    [Theory]\n    [InlineData(1)]\n    [InlineData(2)]\n    public void B(int x) { }\n\n    [NeedsPythonFact]\n    public void C() { }\n}\n");
        world.Write("scripts/b.py", "print(1)\n");
        // A line holding an attribute's form in a page, which is no test.
        world.Write("README.md", "# Sample\n\n[Fact]\n");
        var head = world.Commit(null, null, "The tests of the sample.\nIts second line.");
        var trx = Path.Combine(world.Root, "results.trx");
        File.WriteAllText(trx, "<?xml version=\"1.0\"?>\n<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><ResultSummary outcome=\"Completed\"><Counters total=\"5\" executed=\"4\" passed=\"4\" failed=\"0\" notExecuted=\"1\" /></ResultSummary></TestRun>\n");

        var (code, printed) = Programs.Attempt("report.py", [], world.Repo, "HEAD", "--trx=" + trx);

        Assert.True(code == 0, printed);
        var lines = Programs.Lines(printed);
        Assert.Equal($"`{head[..7]}` The tests of the sample. Its second line.", lines[0]);
        Assert.Contains("files: 3 changed, documents 1, scripts 1, tests 1", lines);
        Assert.Contains("lines: 17 added, 0 removed", lines);
        Assert.Contains("test attributes on added lines of C#: 2 facts, 2 data rows", lines);
        Assert.Contains("tests: 5 total, 4 passed, 0 failed, 1 notExecuted, read from results.trx by its counters, holding no results", lines);
        Assert.Contains(lines, line => line.StartsWith("check: commitcheck.py exit 1, ", StringComparison.Ordinal) && !line.Contains(", 0 FAIL", StringComparison.Ordinal));
    }

    [NeedsPythonFact]
    public void Python_Tests_Under_A_Lowercase_Folder_Are_Tests_And_Their_Functions_Are_Counted()
    {
        using var world = new World();
        world.Commit("a.txt", "a\n", "The first file.");
        world.Write("sheets/tests/test_a.py", "import unittest\n\n\nclass A(unittest.TestCase):\n    def test_one(self):\n        pass\n\n    def test_two(self):\n        pass\n\n    def helper(self):\n        pass\n");
        world.Write("sheets/chart.py", "def test_like_name():\n    pass\n");
        world.Commit(null, null, "Two tests of a chart.");

        var lines = Programs.Lines(Programs.Script("report.py", world.Repo, "--trx=" + Path.Combine(world.Root, "none.trx")));

        Assert.Contains("files: 2 changed, scripts 1, tests 1", lines);
        Assert.Contains("test functions on added lines of Python tests: 2", lines);
    }

    [NeedsPythonFact]
    public void With_Run_The_Block_Gives_The_Commits_Run_And_Its_Jobs_Read_From_Gh()
    {
        using var world = new World();
        var head = world.Commit("a.txt", "a\n", "The first file.");
        world.Remote("https://github.com/EggyStudio/Sample.git");
        var answers = world.Answers(new
        {
            run = new { databaseId = 42, headSha = head, status = "completed", conclusion = "failure" },
            jobs = new object[]
            {
                new { name = "test (ubuntu-24.04)", conclusion = "success", startedAt = "2026-11-18T20:00:00Z", completedAt = "2026-11-18T20:00:56Z" },
                new { name = "test (windows-latest)", conclusion = "failure", startedAt = "2026-11-18T20:00:00Z", completedAt = "2026-11-18T20:01:29Z" },
                // A job still running, which gh gives no conclusion and an end in the year 1.
                new { name = "test (macos-15)", conclusion = "", status = "in_progress", startedAt = "2026-11-18T20:00:00Z", completedAt = "0001-01-01T00:00:00Z" },
            },
        });

        var (code, printed) = Programs.Attempt("report.py", new() { ["REPORT_ANSWERS"] = answers }, world.Repo, "--run", "--trx=" + answers);

        Assert.True(code == 0, printed);
        var lines = Programs.Lines(printed);
        Assert.Contains("run: 42 of EggyStudio/Sample, completed failure", lines);
        Assert.Contains("  test (ubuntu-24.04) success 56 s", lines);
        Assert.Contains("  test (windows-latest) failure 1 min 29 s", lines);
        Assert.Contains("  test (macos-15) in_progress", lines);
    }

    [NeedsPythonFact]
    public void A_Run_Not_Made_Not_Reached_Or_With_No_Remote_Is_Said_So()
    {
        using var world = new World();
        world.Commit("a.txt", "a\n", "The first file.");
        var noRemote = Programs.Lines(Programs.Script("report.py", world.Repo, "--run"));
        world.Remote("git@github.com:EggyStudio/Sample.git");
        var none = world.Answers(new { run = (object?)null });
        var failed = world.Answers(new { failed = "HTTP 503" });

        var noRun = Programs.Lines(Programs.Script("report.py", new Dictionary<string, string> { ["REPORT_ANSWERS"] = none }, world.Repo, "--run"));
        var unreached = Programs.Lines(Programs.Script("report.py", new Dictionary<string, string> { ["REPORT_ANSWERS"] = failed }, world.Repo, "--run"));

        Assert.Contains("run: not read, the repository has no origin on GitHub", noRemote);
        Assert.Contains("run: none of EggyStudio/Sample for this commit yet", noRun);
        Assert.Contains("run: not read, gh run list failed, HTTP 503", unreached);
    }

    [NeedsPythonFact]
    public void A_Trx_Is_Read_By_Its_Results_Outcomes_Where_Its_Counters_Leave_A_Skipped_Test_Out()
    {
        using var world = new World();
        world.Commit("a.txt", "a\n", "The first file.");
        // As dotnet test writes one, a skipped test in its results and in none of its counters.
        var trx = Path.Combine(world.Root, "results.trx");
        File.WriteAllText(trx, "<?xml version=\"1.0\"?>\n<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results>"
            + "<UnitTestResult testName=\"A\" outcome=\"Passed\" /><UnitTestResult testName=\"B\" outcome=\"Passed\" /><UnitTestResult testName=\"C\" outcome=\"NotExecuted\" />"
            + "</Results><ResultSummary outcome=\"Completed\"><Counters total=\"3\" executed=\"2\" passed=\"2\" failed=\"0\" notExecuted=\"0\" /></ResultSummary></TestRun>\n");

        var lines = Programs.Lines(Programs.Script("report.py", world.Repo, "--trx=" + trx));

        Assert.Contains("tests: 3 total, 2 passed, 0 failed, 1 not executed, read from results.trx by its results", lines);
    }

    [NeedsPythonFact]
    public void A_Count_It_Could_Not_Read_Is_Said_Unread_And_A_Commit_Git_Cannot_Read_Gives_2()
    {
        using var world = new World();
        world.Commit("a.txt", "a\n", "The first file.");
        var broken = Path.Combine(world.Root, "broken.trx");
        File.WriteAllText(broken, "<TestRun>");

        var none = Programs.Lines(Programs.Script("report.py", world.Repo));
        var unread = Programs.Lines(Programs.Script("report.py", world.Repo, "--trx=" + broken));
        var (code, printed) = Programs.Attempt("report.py", [], world.Repo, "nothere");

        Assert.Contains("tests: not read, no TRX file is given or under the repository's TestResults", none);
        Assert.Contains(unread, line => line.StartsWith("tests: not read from broken.trx, ", StringComparison.Ordinal));
        Assert.Equal(2, code);
        Assert.Contains("git rev-parse nothere failed", printed);
    }

    // A made repository whose commits are written with a plain message, which the commit's check
    // reads as wanting its marks, deleted with its folder.
    private sealed class World : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("report-").FullName;

        public string Repo => Path.Combine(Root, "Sample");

        public World()
        {
            Directory.CreateDirectory(Repo);
            Git("init", "-q", "-b", "main");
        }

        private string Git(params string[] arguments) =>
            Programs.Run("git", Repo, ["-c", "user.name=EggyStudio", "-c", "user.email=owner@example.com", "-c", "commit.gpgsign=false", "-c", "core.autocrlf=false", .. arguments]);

        public void Write(string path, string text)
        {
            var file = Path.Combine(Repo, path);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, text);
        }

        public string Commit(string? path, string? text, string sentence)
        {
            if (path is not null) Write(path, text!);
            Git("add", "-A");
            File.WriteAllText(Path.Combine(Repo, ".git", "message"), "A subject\n\n" + sentence + "\n");
            Git("commit", "-q", "--cleanup=verbatim", "-F", Path.Combine(Repo, ".git", "message"));
            return Git("rev-parse", "HEAD").Trim();
        }

        public void Remote(string address) => Git("remote", "add", "origin", address);

        // The answers gh would give, written where REPORT_ANSWERS names them for the script.
        public string Answers(object answers)
        {
            var path = Path.Combine(Root, $"answers-{Guid.NewGuid():N}.json");
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(answers));
            return path;
        }

        public void Dispose()
        {
            foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(Root, true);
        }
    }
}
