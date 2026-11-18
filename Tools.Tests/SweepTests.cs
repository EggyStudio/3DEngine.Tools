using Xunit;

namespace Tools.Tests;

public class SweepTests
{
    // A name made up for the test, which stands for the names the sweep is given and no file holds.
    private const string MadeUpName = "Quillon Darrowmere";

    [NeedsPythonFact]
    public void A_Tree_That_Keeps_The_Rules_Passes()
    {
        var (exit, printed) = Sweep(new()
        {
            ["scripts/a.py"] = "# Prints one.\nprint(1)\n",
            ["README.md"] = "# Sample\n\n* " + string.Join(' ', Enumerable.Repeat("word", 25)) + "\n",
        });

        Assert.True(exit == 0, printed);
        Assert.Contains("swept 2 text files and 0 pictures of 2 tracked in ", printed);
        Assert.DoesNotContain("FAIL", printed);
    }

    [NeedsPythonFact]
    public void Each_Rule_Is_Counted_With_Its_First_Ten_Places_And_The_Exit_Code_Says_It_Failed()
    {
        var longComment = "# " + string.Join(' ', Enumerable.Repeat("word", 22)) + "\n";
        var (exit, printed) = Sweep(new()
        {
            ["scripts/long.py"] = string.Concat(Enumerable.Repeat(longComment, 12)) + "print(1)\n",
            ["scripts/prose.py"] = "# The colour of the ground.\n# The value used to be read twice.\nprint(1)\n",
            ["notes.md"] = "A line \u2014 with a dash.\n",
            [$"docs/{MadeUpName}.md"] = "Kept.\n",
            ["docs/thanks.md"] = $"Written for {MadeUpName}.\n",
        },
        untracked: new() { ["scripts/loose.py"] = longComment });

        Assert.Equal(1, exit);
        var lines = printed.Split('\n');
        Assert.Contains("FAIL a comment line past 100 columns, 12", lines);
        Assert.Equal(10, lines.Count(line => line.StartsWith("  scripts/long.py:", StringComparison.Ordinal)));
        Assert.Contains("FAIL British spelling, 1", lines);
        Assert.Contains("WARN a comment tells what the code was, 1", lines);
        Assert.Contains("FAIL em dash, 1", lines);
        Assert.Contains("FAIL a forbidden name, 2", lines);
        Assert.DoesNotContain(MadeUpName, printed);
        Assert.DoesNotContain("loose.py", printed);
    }

    [NeedsPythonFact]
    public void A_Plain_Earlier_And_A_Workflows_Runner_Are_No_Account_Of_The_Code_Or_A_Run()
    {
        var (_, printed) = Sweep(new()
        {
            ["scripts/earlier.py"] = "# The earlier command's output is read.\n# An earlier version read it twice.\nprint(1)\n",
            [".github/workflows/build.yml"] = "# The runner is Ubuntu's.\n# The build took 8 ms on a desktop.\non: push\n",
            ["Library/A.cs"] = "// Read on the runner.\nclass A {}\n",
        });

        var lines = printed.Split('\n');
        Assert.Contains("WARN a comment tells what the code was, 1", lines);
        Assert.Contains("  scripts/earlier.py:2 a comment tells what the code was, \"An earlier version\", in # An earlier version read it twice.", lines);
        Assert.Contains("WARN a comment carries a run's facts, 2", lines);
        Assert.DoesNotContain(".github/workflows/build.yml:1", printed);
    }

    [NeedsPythonFact]
    public void Where_No_Names_Are_Given_The_Sweep_Says_It_Did_Not_Read_For_Them()
    {
        var (exit, printed) = Sweep(new() { ["docs/thanks.md"] = $"Written for {MadeUpName}.\n" }, names: null);

        Assert.Equal(0, exit);
        Assert.Contains("WARN FORBIDDEN_NAMES is not set, so the names were not read for", printed);
    }

    [NeedsPillowFact]
    public void A_Picture_Under_The_Readings_On_A_Dark_Ground_Fails()
    {
        var (exit, printed) = Sweep(new()
        {
            [".github/assets/readings/2026-11-18-item-1-dark.webp"] = "black",
            [".github/assets/readings/2026-11-18-item-1-white.webp"] = "white",
        });

        Assert.Equal(1, exit);
        Assert.Contains("FAIL a picture's ground is not white, 1", printed.Split('\n'));
        Assert.Contains("  .github/assets/readings/2026-11-18-item-1-dark.webp the ground is not white", printed);
        Assert.Contains("swept 0 text files and 2 pictures of 2 tracked", printed);
    }

    // A repository whose files given are tracked, the untracked ones written beside them, a
    // picture's text naming its color, and the sweep run over it with the made-up name or none.
    private static (int Code, string Printed) Sweep(Dictionary<string, string> files, Dictionary<string, string>? untracked = null, string? names = MadeUpName)
    {
        var repo = Directory.CreateTempSubdirectory("sweep-").FullName;
        try
        {
            Git(repo, "init", "-q");
            Write(repo, files);
            Git(repo, "add", "-A");
            Write(repo, untracked ?? []);
            return Programs.Attempt("sweep.py", new() { ["FORBIDDEN_NAMES"] = names }, repo);
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(repo, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(repo, true);
        }
    }

    private static void Write(string repo, Dictionary<string, string> files)
    {
        foreach (var (path, text) in files)
        {
            var file = Path.Combine(repo, path);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            if (path.EndsWith(".webp", StringComparison.Ordinal))
                Programs.Run(Programs.Python.Value!, repo, "-I", "-c", $"from PIL import Image; Image.new('RGB', (64, 32), '{text}').save(r'{file}')");
            else
                File.WriteAllText(file, text);
        }
    }

    private static string Git(string repo, params string[] arguments) =>
        Programs.Run("git", repo, ["-c", "core.autocrlf=false", .. arguments]);
}
