using Xunit;

namespace Tools.Tests;

public class DailyTests
{
    // A name made up for the test, which stands for the names the sweep is given and no file holds.
    private const string MadeUpName = "Quillon Darrowmere";

    private static readonly string LongComment = "# " + string.Join(' ', Enumerable.Repeat("word", 22)) + "\n";

    [NeedsPythonFact]
    public void A_Report_Counts_Each_Repositorys_Rules_And_Top_Folders_And_Leaves_The_Repositories_As_They_Were()
    {
        using var day = new Day();
        var faulty = day.Repository("faulty", new()
        {
            ["scripts/a.py"] = LongComment + LongComment + LongComment + "print(1)\n",
            ["build/b.py"] = LongComment + "print(1)\n",
            ["notes.md"] = "A line — with a dash.\n",
            ["src/c.py"] = "# The value used to be read twice.\nprint(1)\n",
        });
        var clean = day.Repository("clean", new() { ["scripts/a.py"] = "# Prints one.\nprint(1)\n" });
        var before = (day.Status(faulty), day.Status(clean));

        var (exit, report) = day.Report("day.md", null, faulty, clean);

        Assert.Equal(0, exit);
        var lines = Programs.Lines(report);
        Assert.Contains(lines, l => l.StartsWith("| faulty | 5 | 1 |  |  |", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.StartsWith("| clean | 0 | 0 |", StringComparison.Ordinal));
        Assert.Contains("| FAIL | a comment line past 100 columns | 4 |  |", lines);
        Assert.Contains("| FAIL | em dash | 1 |  |", lines);
        Assert.Contains("| WARN | a comment tells what the code was | 1 |  |", lines);
        Assert.Contains("| `scripts` | 3 | 0 |", lines);
        Assert.Contains("| `build` | 1 | 0 |", lines);
        Assert.Contains("| `(the root)` | 1 | 0 |", lines);
        Assert.Contains("| `src` | 0 | 1 |", lines);
        Assert.Contains("| `scripts/a.py` | 3 |", lines);
        Assert.Contains("| | no rule broken | 0 | |", lines);
        Assert.Equal(before, (day.Status(faulty), day.Status(clean)));
    }

    [NeedsPythonFact]
    public void A_Report_Given_The_One_Before_Gives_The_Change_In_Each_Count_And_A_Rule_No_Longer_Broken()
    {
        using var day = new Day();
        var repo = day.Repository("repo", new() { ["a.py"] = LongComment + "print(1)\n", ["notes.md"] = "A line — with a dash.\n" });
        var (_, _) = day.Report("first.md", null, repo);
        day.Write(repo, "a.py", LongComment + LongComment + LongComment + "print(1)\n");
        day.Write(repo, "notes.md", "A line, with a comma.\n");

        var (exit, report) = day.Report("second.md", "first.md", repo);

        Assert.Equal(0, exit);
        var lines = Programs.Lines(report);
        Assert.Contains("| FAIL | a comment line past 100 columns | 3 | +2 |", lines);
        Assert.Contains("| FAIL | em dash | 0 | -1 |", lines);
        Assert.Contains(lines, l => l.StartsWith("| repo | 3 | 0 | +1 | 0 |", StringComparison.Ordinal));
    }

    [NeedsPythonFact]
    public void Each_Rule_And_Folder_Is_Counted_Largest_First_With_What_The_Rewrap_Would_Clear()
    {
        using var day = new Day();
        // Three lines of prose the rewrap fills, a comment's indented code it leaves, and a dash no
        // rewrap mends.
        var indented = "#     " + string.Join(' ', Enumerable.Repeat("code", 20)) + "\n";
        var repo = day.Repository("repo", new()
        {
            ["scripts/a.py"] = LongComment + LongComment + LongComment + "print(1)\n",
            ["scripts/b.py"] = indented + "print(1)\n",
            ["src/c.py"] = "# A line \u2014 with a dash.\nprint(1)\n",
        });
        var before = day.Status(repo);

        var (_, report) = day.Report("day.md", null, repo);

        var lines = Programs.Lines(report);
        var table = Array.IndexOf(lines, "| level | rule | top folder | count | the rewrap clears | for hands |");
        Assert.True(table > 0, "the report has no table by rule and folder");
        Assert.Equal("| FAIL | a comment line past 100 columns | `scripts` | 4 | 3 | 1 |", lines[table + 2]);
        Assert.Equal("| FAIL | em dash | `src` | 1 | 0 | 1 |", lines[table + 3]);
        Assert.Contains("Of 5 failures the rewrap would clear 3, and 2 are for hands.", lines);
        Assert.Equal(before, day.Status(repo));
    }

    [NeedsPythonFact]
    public void The_Ten_Files_A_Rule_Holds_Most_Of_Are_Listed_And_The_Rest_Counted()
    {
        using var day = new Day();
        var files = Enumerable.Range(1, 12).ToDictionary(i => $"scripts/f{i:00}.py", i => string.Concat(Enumerable.Repeat(LongComment, i)) + "print(1)\n");
        var repo = day.Repository("repo", files);

        var (_, report) = day.Report("day.md", null, repo);

        var lines = Programs.Lines(report);
        Assert.Contains("| `scripts/f12.py` | 12 |", lines);
        Assert.Contains("| `scripts/f03.py` | 3 |", lines);
        Assert.DoesNotContain("| `scripts/f02.py` | 2 |", lines);
        Assert.Contains("| 2 files more | 3 |", lines);
    }

    [NeedsPythonFact]
    public void A_Name_Is_Counted_And_Written_Nowhere_In_The_Report()
    {
        using var day = new Day();
        var repo = day.Repository("repo", new() { [$"docs/{MadeUpName}.md"] = "Kept.\n", ["docs/thanks.md"] = $"Written for {MadeUpName}.\n" });

        var (_, report) = day.ReportWithNames("day.md", repo, MadeUpName);

        Assert.Contains("| FAIL | a forbidden name | 2 |  |", Programs.Lines(report));
        Assert.DoesNotContain(MadeUpName, report);
        Assert.Contains("`docs/<a name>.md`", report);
    }

    [NeedsPythonFact]
    public void A_Previous_File_That_Is_No_Report_Is_Refused_By_Name()
    {
        using var day = new Day();
        var repo = day.Repository("repo", new() { ["a.py"] = "print(1)\n" });
        File.WriteAllText(Path.Combine(day.Folder, "notes.md"), "# Notes\n");

        var (exit, printed) = Programs.Attempt("daily.py", new() { ["FORBIDDEN_NAMES"] = null },
            Path.Combine(day.Folder, "day.md"), "--previous=" + Path.Combine(day.Folder, "notes.md"), repo);

        Assert.Equal(1, exit);
        Assert.Contains("carries no counts, so it is no report this script wrote", printed);
        Assert.False(File.Exists(Path.Combine(day.Folder, "day.md")));
    }

    // A folder of made repositories, each tracking the files given, and the reports written beside
    // them, deleted with the folder.
    private sealed class Day : IDisposable
    {
        public string Folder { get; } = Directory.CreateTempSubdirectory("daily-").FullName;

        public string Repository(string name, Dictionary<string, string> files)
        {
            var repo = Path.Combine(Folder, name);
            Directory.CreateDirectory(repo);
            Git(repo, "init", "-q");
            foreach (var (path, text) in files) Write(repo, path, text);
            Git(repo, "add", "-A");
            return repo;
        }

        public void Write(string repo, string path, string text)
        {
            var file = Path.Combine(repo, path);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, text);
        }

        public string Status(string repo) => Git(repo, "status", "--porcelain");

        public (int Code, string Report) Report(string name, string? previous, params string[] repos) =>
            Report(name, previous, repos, names: null);

        public (int Code, string Report) ReportWithNames(string name, string repo, string names) =>
            Report(name, null, [repo], names);

        private (int Code, string Report) Report(string name, string? previous, string[] repos, string? names)
        {
            var written = Path.Combine(Folder, name);
            string[] arguments = previous is null ? [written, .. repos] : [written, "--previous=" + Path.Combine(Folder, previous), .. repos];
            var (code, printed) = Programs.Attempt("daily.py", new() { ["FORBIDDEN_NAMES"] = names }, arguments);
            Assert.True(code == 0, printed);
            return (code, File.ReadAllText(written));
        }

        public void Dispose()
        {
            foreach (var file in Directory.EnumerateFiles(Folder, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(Folder, true);
        }

        private static string Git(string repo, params string[] arguments) =>
            Programs.Run("git", repo, ["-c", "core.autocrlf=false", .. arguments]);
    }
}
