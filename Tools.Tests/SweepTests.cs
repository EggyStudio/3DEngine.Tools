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
        var lines = Programs.Lines(printed);
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
            ["Library.Tests/A.cs"] = "// Read on the runner.\nclass A {}\n",
        });

        var lines = Programs.Lines(printed);
        Assert.Contains("WARN a comment tells what the code was, 1", lines);
        Assert.Contains("  scripts/earlier.py:2 a comment tells what the code was, \"An earlier version\", in # An earlier version read it twice.", lines);
        Assert.Contains("WARN a comment carries a run's facts, 2", lines);
        Assert.DoesNotContain(".github/workflows/build.yml:1", printed);
    }

    // Each form the two rules read for, with a line that tells of the code or a run and a line of
    // the same words that does not, the second warned of nowhere.
    [Theory]
    [InlineData("scripts/a.py", "# It used to read twice.", "# The value is used to scale the frame.")]
    [InlineData("scripts/a.py", "# The frame used to be read twice.", "# Which way is up, used to settle the roll.")]
    [InlineData("scripts/a.py", "# Before this change the frame was read twice.", "# The camera stands before this frame.")]
    [InlineData("scripts/a.py", "# It no longer reads twice.", "# What had been allocated is counted.")]
    [InlineData("Library.Tests/A.cs", "// Read on the runner.", "// Read on the runner.")]
    public void A_Form_Is_Warned_Of_Where_It_Tells_Of_The_Code_Or_A_Run_And_Not_Where_Its_Words_Are_Plain(string path, string told, string plain)
    {
        // The word names a run's machine in a test's comment and a generated class in a library's.
        var plainPath = path == "Library.Tests/A.cs" ? "Library/A.cs" : "scripts/b.py";
        var mark = path.EndsWith(".cs", StringComparison.Ordinal) ? "class A {}" : "print(1)";

        var (_, printed) = Sweep(new() { [path] = $"{told}\n{mark}\n", [plainPath] = $"{plain}\n{mark}\n" });

        Assert.Contains($"{path}:1 a comment", printed);
        Assert.DoesNotContain($"{plainPath}:1", printed);
    }

    [NeedsPythonFact]
    public void A_Line_Of_Code_That_Opens_With_A_Star_Or_A_Closed_Block_Comment_Is_No_Comment_Line()
    {
        string Many(string word, int count) => string.Join(' ', Enumerable.Repeat(word, count));
        var source = string.Join('\n',
            "class A",
            "{",
            "    float M(float a, float b) => a",
            $"        * {string.Join(" * ", Enumerable.Repeat("b", 50))};",
            $"    /* 0 */ [{string.Join(",", Enumerable.Repeat("1", 50))}],",
            $"    /* {Many("open", 25)}",
            $"     * {Many("inner", 20)}",
            "     */",
            $"    /* {Many("closed", 16)} */",
            "}",
            "");

        var (_, printed) = Sweep(new() { ["A.cs"] = source });

        var lines = Programs.Lines(printed);
        Assert.Contains("FAIL a comment line past 100 columns, 3", lines);
        Assert.Contains(lines, line => line.StartsWith("  A.cs:6 ", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("  A.cs:7 ", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.StartsWith("  A.cs:9 ", StringComparison.Ordinal));
    }

    [NeedsPythonFact]
    public void A_Place_Holding_A_Character_Past_Ascii_Is_Printed_Whole_Whatever_The_Systems_Encoding()
    {
        var (exit, printed) = Sweep(new() { ["notes.md"] = "A line \u2014 with a dash.\n" }, environment: Programs.SingleByteLocale());

        Assert.Equal(1, exit);
        Assert.Contains("  notes.md:1 em dash, \"\u2014\"", Programs.Lines(printed));
    }

    [NeedsPythonFact]
    public void A_Submodules_Files_Are_Not_Read()
    {
        var repo = Directory.CreateTempSubdirectory("sweep-").FullName;
        try
        {
            // A repository holding another under modules/Tools as its .gitmodules names, which git
            // tracks as the commit it points at, its long comment read in its own checkout alone.
            var nested = Path.Combine(repo, "modules", "Tools");
            Directory.CreateDirectory(nested);
            Git(nested, "init", "-q");
            File.WriteAllText(Path.Combine(nested, "a.py"), "# " + string.Join(' ', Enumerable.Repeat("word", 30)) + "\n");
            Git(nested, "add", "-A");
            Git(nested, "-c", "user.name=Tools", "-c", "user.email=tools@example.com", "-c", "commit.gpgsign=false", "commit", "-q", "-m", "The nested commit.");
            Git(repo, "init", "-q");
            File.WriteAllText(Path.Combine(repo, ".gitmodules"), "[submodule \"modules/Tools\"]\n\tpath = modules/Tools\n\turl = https://example.com/Tools.git\n");
            File.WriteAllText(Path.Combine(repo, "b.py"), "print(1)\n");
            Git(repo, "add", "-A");

            var (exit, printed) = Programs.Attempt("sweep.py", new() { ["FORBIDDEN_NAMES"] = null }, repo);

            Assert.True(exit == 0, printed);
            Assert.DoesNotContain("modules/Tools", printed);
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(repo, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(repo, true);
        }
    }

    [NeedsPythonFact]
    public void The_Cheatsheet_Written_As_Two_Words_Fails_On_Any_Line()
    {
        // The two words are joined here from parts, so this file holds them on no line.
        string Split(string first) => first + "heat" + " sheet";
        var (exit, printed) = Sweep(new()
        {
            ["README.md"] = $"# Sample\n\nThe {Split("c")} lists the types.\nThe cheatsheet is written.\n",
            ["Sample/A.cs"] = $"class A {{ string Title = \"{Split("C")}\"; }}\n",
        });

        Assert.Equal(1, exit);
        Assert.Contains("FAIL the cheatsheet written as two words, 2", Programs.Lines(printed));
        Assert.DoesNotContain("README.md:4", printed);
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
        Assert.Contains("FAIL a picture's ground is not white, 1", Programs.Lines(printed));
        Assert.Contains("  .github/assets/readings/2026-11-18-item-1-dark.webp the ground is not white", printed);
        Assert.Contains("swept 0 text files and 2 pictures of 2 tracked", printed);
    }

    [NeedsPillowFact]
    public void A_Standing_Picture_Its_Folders_Captions_Give_No_Line_Is_Warned_Of_And_Counted()
    {
        var (exit, printed) = Sweep(new()
        {
            [".github/assets/readings/2026-11-18-item-1-room.webp"] = "white",
            [".github/assets/readings/2026-11-18-item-1-hall.webp"] = "white",
            [".github/assets/readings/captions.md"] = "2026-11-18-item-1-room.webp | The room at noon.\n",
            [".github/devlog/2026-11-18-item-2-field.webp"] = "white",
        });

        Assert.True(exit == 0, printed);
        Assert.Contains("WARN a picture its folder's captions.md gives no line, 2", Programs.Lines(printed));
        Assert.Contains("  .github/assets/readings/2026-11-18-item-1-hall.webp has no line in .github/assets/readings/captions.md", printed);
        Assert.Contains("  .github/devlog/2026-11-18-item-2-field.webp has no line in .github/devlog/captions.md", printed);
        Assert.DoesNotContain("item-1-room.webp has no line", printed);
    }

    // A repository whose files given are tracked, the untracked ones written beside them, a
    // picture's text naming its color, and the sweep run over it with the made-up name or none.
    private static (int Code, string Printed) Sweep(Dictionary<string, string> files, Dictionary<string, string>? untracked = null, string? names = MadeUpName, Dictionary<string, string?>? environment = null)
    {
        var repo = Directory.CreateTempSubdirectory("sweep-").FullName;
        try
        {
            Git(repo, "init", "-q");
            Write(repo, files);
            Git(repo, "add", "-A");
            Write(repo, untracked ?? []);
            return Programs.Attempt("sweep.py", new(environment ?? []) { ["FORBIDDEN_NAMES"] = names }, repo);
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
