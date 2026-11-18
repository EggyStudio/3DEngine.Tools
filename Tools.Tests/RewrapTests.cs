using Xunit;

namespace Tools.Tests;

public class RewrapTests
{
    private static readonly string Words = string.Join(' ', Enumerable.Repeat("word", 30));

    [NeedsPythonFact]
    public void Only_The_Repositorys_Tracked_Files_Are_Read_And_The_Count_Is_The_Sweeps()
    {
        var comment = $"class A\n{{\n    // {Words}\n}}\n";

        // An ignored file, an untracked one and a repository of its own inside the checkout, which
        // git tracks as the commit it points at and none of its files.
        var files = new Dictionary<string, string> { ["A.cs"] = comment, [".gitignore"] = "ignored.cs\n" };
        var untracked = new Dictionary<string, string> { ["ignored.cs"] = comment, ["loose.cs"] = comment };

        var (exit, printed, after) = RewrapWithNested(files, untracked, comment);

        Assert.Equal(0, exit);
        Assert.Contains("1 comment lines past 100 columns in 1 files rewrapped, 0 left as they stand, 0 files refused", printed);
        Assert.NotEqual(comment, after["A.cs"]);
        Assert.Equal(comment, after["ignored.cs"]);
        Assert.Equal(comment, after["loose.cs"]);
        Assert.Equal(comment, after["nested/B.cs"]);
        Assert.Contains("FAIL a comment line past 100 columns, 1", after["sweep"]);
    }

    // As Rewrap, with a repository of its own committed under nested/ before the outer one adds it,
    // and the sweep's output over the outer one taken first, under the name sweep.
    private static (int Code, string Printed, Dictionary<string, string> After) RewrapWithNested(
        Dictionary<string, string> files, Dictionary<string, string> untracked, string nestedComment)
    {
        var repo = Directory.CreateTempSubdirectory("rewrap-").FullName;
        try
        {
            Git(repo, "init", "-q");
            var nested = Path.Combine(repo, "nested");
            Directory.CreateDirectory(nested);
            Git(nested, "init", "-q");
            Write(nested, new() { ["B.cs"] = nestedComment });
            Git(nested, "add", "-A");
            Git(nested, "commit", "-q", "-m", "The nested commit.");
            Write(repo, files);
            Git(repo, "add", "-A");
            Write(repo, untracked);
            var (_, swept) = Programs.Attempt("sweep.py", new() { ["FORBIDDEN_NAMES"] = null }, repo);
            var (code, printed) = Programs.Attempt("rewrap.py", [], "--write", repo);
            var after = files.Keys.Concat(untracked.Keys).Append("nested/B.cs").ToDictionary(name => name, name => File.ReadAllText(Path.Combine(repo, name)));
            after["sweep"] = swept;
            return (code, printed, after);
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(repo, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(repo, true);
        }
    }

    [NeedsPythonAndSymbolicLinksFact]
    public void A_Repository_Reached_Through_A_Symbolic_Link_Is_Read_As_Through_Its_Own_Path()
    {
        // macOS reaches its temporary folder through a link, /var to /private/var, and git names a
        // repository's top by the path the link leads to, so a path given through the link and the
        // top git gives must be read alike.
        var real = Directory.CreateTempSubdirectory("rewrap-real-").FullName;
        var linked = real + "-link";
        try
        {
            Git(real, "init", "-q");
            Write(real, new() { ["A.cs"] = $"class A\n{{\n    // {Words}\n}}\n" });
            Git(real, "add", "-A");
            Directory.CreateSymbolicLink(linked, real);

            var (exit, printed) = Programs.Attempt("rewrap.py", [], "--write", linked);

            Assert.True(exit == 0, printed);
            Assert.Contains("1 comment lines past 100 columns in 1 files rewrapped, 0 left as they stand, 0 files refused", printed);
            Assert.All(File.ReadAllLines(Path.Combine(real, "A.cs")), line => Assert.True(line.Length <= 100, line));
        }
        finally
        {
            Directory.Delete(linked);
            foreach (var file in Directory.EnumerateFiles(real, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(real, true);
        }
    }

    [NeedsPythonFact]
    public void A_Dry_Run_Counts_The_Lines_And_Changes_No_File()
    {
        var files = new Dictionary<string, string> { ["A.cs"] = $"class A\n{{\n    // {Words}\n}}\n", ["b.py"] = $"# {Words}\n# {Words}\nprint(1)\n" };

        var (exit, printed, after) = Rewrap(files, write: false);

        Assert.Equal(0, exit);
        Assert.Contains("3 comment lines past 100 columns in 2 files would be rewrapped, 0 left as they stand, 0 files refused", printed);
        Assert.Equal(files, after);
    }

    [NeedsPythonFact]
    public void A_Summary_A_List_And_A_Line_Comment_Are_Filled_To_The_Width_With_Their_Words_And_Tags_Whole()
    {
        // The file's lines are built with their marks as values, so no line of this test's own
        // source opens with a comment's mark and runs past the width.
        const string Doc = "///", Line = "    //";
        var source = Join(
            $"{Doc} <summary>", $"{Doc} {Words} <see cref=\"Script(string, string[])\"/> and more.", $"{Doc} </summary>",
            $"{Doc} <remarks>", $"{Doc} - {Words}", $"{Doc}   continued.", $"{Doc} </remarks>",
            "class A", "{", $"{Line} a < b and {Words}", Line, $"{Line}     indented code stays as it is.", $"    var x = 1; // {Words}", "}");

        var (_, _, after) = Rewrap(new() { ["A.cs"] = source }, write: true);

        string Of(int count) => string.Join(' ', Enumerable.Repeat("word", count));
        Assert.Equal(Join(
            $"{Doc} <summary>", $"{Doc} {Of(19)}", $"{Doc} {Of(11)} <see cref=\"Script(string, string[])\"/>", $"{Doc} and more.", $"{Doc} </summary>",
            $"{Doc} <remarks>", $"{Doc} - {Of(19)}", $"{Doc}   {Of(11)} continued.", $"{Doc} </remarks>",
            "class A", "{", $"{Line} a < b and {Of(16)}", $"{Line} {Of(14)}", Line, $"{Line}     indented code stays as it is.", $"    var x = 1; // {Words}", "}"),
            after["A.cs"]);
    }

    [NeedsPythonFact]
    public void A_Table_In_A_Comment_Keeps_Its_Rows_Beside_A_Paragraph_That_Is_Filled()
    {
        var row = "# | " + string.Join(" | ", Enumerable.Repeat("cell", 25)) + " |\n";
        var table = "# | a | b |\n# |---|---|\n" + row;
        var (_, printed, after) = Rewrap(new() { ["b.py"] = $"# {Words}\n{table}print(1)\n" }, write: true);

        var lines = after["b.py"].Split('\n');
        Assert.Equal(["# | a | b |", "# |---|---|", row.TrimEnd('\n')], lines[^5..^2]);
        Assert.Contains("1 comment lines past 100 columns in 1 files rewrapped, 1 left as they stand", printed);
    }

    [NeedsPythonFact]
    public void A_Python_Comment_And_An_Xml_Comment_Are_Filled_And_A_Shebang_Is_Kept()
    {
        var (_, _, after) = Rewrap(new()
        {
            ["b.py"] = $"#!/usr/bin/env python3\n# {Words}\nprint(1)\n",
            ["c.csproj"] = $"<Project>\n    <!-- {Words}\n         the end. -->\n</Project>\n",
        }, write: true);

        // Nineteen words and the mark fill 96 columns, and a twentieth would reach 101.
        var (first, second) = (string.Join(' ', Enumerable.Repeat("word", 19)), string.Join(' ', Enumerable.Repeat("word", 11)));
        Assert.Equal($"#!/usr/bin/env python3\n# {first}\n# {second}\nprint(1)\n", after["b.py"]);
        Assert.Equal("<Project>\n    <!-- word word word word word word word word word word word word word word word word word word\n         word word word word word word word word word word word word the end. -->\n</Project>\n", after["c.csproj"]);
        Assert.All(after.Values.SelectMany(text => text.Split('\n')), line => Assert.True(line.Length <= 100, line));
    }

    [NeedsPythonFact]
    public void A_Word_Longer_Than_The_Width_Stands_On_A_Line_Of_Its_Own_Which_The_Width_Leaves_Free()
    {
        var address = "https://example.com/" + new string('a', 100);

        var (exit, printed, after) = Rewrap(new() { ["b.py"] = $"# See {address} for the rest of it.\nprint(1)\n" }, write: true);

        Assert.Equal(0, exit);
        Assert.Equal($"# See\n# {address}\n# for the rest of it.\nprint(1)\n", after["b.py"]);
        // An address alone on its line is free of the width, as the sweep reads it, so the line is
        // rewrapped and none is left.
        Assert.Contains("1 comment lines past 100 columns in 1 files rewrapped, 0 left as they stand", printed);
    }

    [NeedsPythonFact]
    public void A_Rewrap_That_Would_Lose_A_Word_Refuses_The_File_And_Leaves_It_As_It_Is()
    {
        // A copy of the script whose filling drops each paragraph's last word, a fault the check
        // of the comments' words exists to stop.
        var brokenFolder = Directory.CreateTempSubdirectory("rewrap-broken-").FullName;
        var broken = Path.Combine(brokenFolder, "rewrap.py");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "scripts", "commitcheck.py"), Path.Combine(brokenFolder, "commitcheck.py"));
        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scripts", "rewrap.py"));
        Assert.Contains("for i, token in enumerate(tokens):", script);
        File.WriteAllText(broken, script.Replace("for i, token in enumerate(tokens):", "for i, token in enumerate(tokens[:-1]):", StringComparison.Ordinal));
        var files = new Dictionary<string, string> { ["b.py"] = $"# {Words} last.\nprint(1)\n" };

        var (exit, printed, after) = Rewrap(files, write: true, script: broken);

        Assert.Equal(1, exit);
        Assert.Contains("b.py refused, since its comments' words would differ", printed);
        Assert.Equal(files, after);
    }

    private static string Join(params string[] lines) => string.Join('\n', lines) + "\n";

    // A repository whose files given are tracked, the untracked ones written beside them after, the
    // script run over its root, and every file as it leaves them.
    private static (int Code, string Printed, Dictionary<string, string> After) Rewrap(
        Dictionary<string, string> files, bool write, string script = "rewrap.py", Dictionary<string, string>? untracked = null)
    {
        var repo = Directory.CreateTempSubdirectory("rewrap-").FullName;
        try
        {
            Git(repo, "init", "-q");
            Write(repo, files);
            Git(repo, "add", "-A");
            Write(repo, untracked ?? []);
            string[] arguments = write ? ["--write", repo] : [repo];
            var (code, printed) = Programs.Attempt(script, [], arguments);
            var all = files.Keys.Concat((untracked ?? []).Keys);
            return (code, printed, all.ToDictionary(name => name, name => File.ReadAllText(Path.Combine(repo, name))));
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(repo, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(repo, true);
        }
    }

    private static void Write(string repo, Dictionary<string, string> files)
    {
        foreach (var (name, text) in files)
        {
            var file = Path.Combine(repo, name);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, text);
        }
    }

    private static string Git(string repo, params string[] arguments) =>
        Programs.Run("git", repo, ["-c", "core.autocrlf=false", "-c", "user.name=Tools", "-c", "user.email=tools@example.com", "-c", "commit.gpgsign=false", .. arguments]);
}
