using Xunit;

namespace Tools.Tests;

public class RewrapTests
{
    private static readonly string Words = string.Join(' ', Enumerable.Repeat("word", 30));

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
    public void A_Word_Longer_Than_The_Width_Stands_On_A_Line_Of_Its_Own_And_Is_Counted_As_Left()
    {
        var address = "https://example.com/" + new string('a', 100);

        var (exit, printed, after) = Rewrap(new() { ["b.py"] = $"# See {address} for the rest of it.\nprint(1)\n" }, write: true);

        Assert.Equal(0, exit);
        Assert.Equal($"# See\n# {address}\n# for the rest of it.\nprint(1)\n", after["b.py"]);
        Assert.Contains("1 left as they stand", printed);
    }

    [NeedsPythonFact]
    public void A_Rewrap_That_Would_Lose_A_Word_Refuses_The_File_And_Leaves_It_As_It_Is()
    {
        // A copy of the script whose filling drops each paragraph's last word, a fault the check
        // of the comments' words exists to stop.
        var broken = Path.Combine(Directory.CreateTempSubdirectory("rewrap-broken-").FullName, "rewrap.py");
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

    // The files written in a folder of their own, the script run over the folder, and the files as
    // it leaves them.
    private static (int Code, string Printed, Dictionary<string, string> After) Rewrap(Dictionary<string, string> files, bool write, string script = "rewrap.py")
    {
        var folder = Directory.CreateTempSubdirectory("rewrap-").FullName;
        try
        {
            foreach (var (name, text) in files) File.WriteAllText(Path.Combine(folder, name), text);
            string[] arguments = write ? ["--write", folder] : [folder];
            var (code, printed) = Programs.Attempt(script, [], arguments);
            return (code, printed, files.Keys.ToDictionary(name => name, name => File.ReadAllText(Path.Combine(folder, name))));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
