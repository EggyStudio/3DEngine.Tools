using Xunit;

namespace Tools.Tests;

public class CommitCheckTests
{
    private const string Lead = "\u200e \u200e \u200e\n\n";

    // A name made up for the test, which stands for the names the check is given and no file holds.
    private const string MadeUpName = "Quillon Darrowmere";

    [NeedsPythonFact]
    public void A_Commit_In_The_Form_Passes()
    {
        var (code, printed) = Check(new() { ["scripts/a.py"] = "# Prints one.\nprint(1)\n" }, Lead + "A script prints one.\n");

        Assert.True(code == 0, printed);
        Assert.Contains(" ok  A script prints one.", printed);
    }

    [NeedsPythonFact]
    public void A_Range_Whose_Earlier_Commit_Fails_Exits_With_1_And_A_Range_Of_Warnings_Alone_With_0()
    {
        var repo = Directory.CreateTempSubdirectory("commitcheck-").FullName;
        try
        {
            Git(repo, "init", "-q");
            Commit(repo, "a.txt", "Add a file.\n\nA file holds a letter.\n");
            Commit(repo, "b.txt", Lead + "A second file holds a letter.\n");

            var (failed, printed) = Programs.Attempt("commitcheck.py", new() { ["FORBIDDEN_NAMES"] = null }, repo, "HEAD~1", "HEAD");
            var (passed, warned) = Programs.Attempt("commitcheck.py", new() { ["FORBIDDEN_NAMES"] = null }, repo, "HEAD");

            Assert.True(failed == 1, printed);
            Assert.True(passed == 0, warned);
            Assert.Contains("WARN FORBIDDEN_NAMES is not set", warned);
        }
        finally
        {
            Delete(repo);
        }
    }

    [NeedsPythonFact]
    public void A_Branch_Under_Claude_Fails_With_1()
    {
        var repo = Directory.CreateTempSubdirectory("commitcheck-").FullName;
        try
        {
            Git(repo, "init", "-q");
            Commit(repo, "a.txt", Lead + "A file holds a letter.\n");
            Git(repo, "checkout", "-q", "-b", "claude/sample");

            var (code, printed) = Programs.Attempt("commitcheck.py", new() { ["FORBIDDEN_NAMES"] = MadeUpName }, repo, "HEAD");

            Assert.True(code == 1, printed);
            Assert.Contains("FAIL the branch is claude/sample", printed);
        }
        finally
        {
            Delete(repo);
        }
    }

    [NeedsPythonFact]
    public void A_Check_That_Cannot_Run_Exits_With_2_And_Says_Why()
    {
        var repo = Directory.CreateTempSubdirectory("commitcheck-").FullName;
        try
        {
            Git(repo, "init", "-q");
            Commit(repo, "a.txt", Lead + "A file holds a letter.\n");

            var (revision, unknown) = Programs.Attempt("commitcheck.py", new() { ["FORBIDDEN_NAMES"] = MadeUpName }, repo, "no-such-commit");
            var (pattern, broken) = Programs.Attempt("commitcheck.py", new() { ["FORBIDDEN_NAMES"] = "(unclosed" }, repo, "HEAD");

            Assert.True(revision == 2, unknown);
            Assert.Contains("git rev-parse no-such-commit failed", unknown);
            Assert.True(pattern == 2, broken);
            Assert.Contains("FORBIDDEN_NAMES is no regular expression", broken);
        }
        finally
        {
            Delete(repo);
        }
    }

    private static void Commit(string repo, string path, string message)
    {
        File.WriteAllText(Path.Combine(repo, path), "a\n");
        Git(repo, "add", "-A");
        File.WriteAllText(Path.Combine(repo, ".git", "message"), message);
        Git(repo, "commit", "-q", "--cleanup=verbatim", "-F", Path.Combine(repo, ".git", "message"));
    }

    private static void Delete(string repo)
    {
        foreach (var file in Directory.EnumerateFiles(repo, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(repo, true);
    }

    [NeedsPythonFact]
    public void A_Line_Naming_An_Engine_Fails_In_A_Module_And_Not_In_Tools_Or_Elsewhere()
    {
        var parent = Directory.CreateTempSubdirectory("commitcheck-").FullName;
        try
        {
            (int, string) Commit(string at)
            {
                var repo = Path.Combine(parent, at);
                Directory.CreateDirectory(repo);
                Git(repo, "init", "-q");
                Directory.CreateDirectory(Path.Combine(repo, "Sample"));
                Directory.CreateDirectory(Path.Combine(repo, ".github"));
                File.WriteAllText(Path.Combine(repo, "README.md"), "# Sample\n\nIt was drawn in BevyCSharp first.\n");
                File.WriteAllText(Path.Combine(repo, "Sample", "Sample.csproj"), "<Project><PropertyGroup><AssemblyName>3DEngine.Sample</AssemblyName></PropertyGroup></Project>\n");
                File.WriteAllText(Path.Combine(repo, ".github", "REVIEW.md"), "3DEngine's item 136.\n");
                Git(repo, "add", "-A");
                File.WriteAllText(Path.Combine(repo, ".git", "message"), Lead + "A module holds a sample.\n");
                Git(repo, "commit", "-q", "--cleanup=verbatim", "-F", Path.Combine(repo, ".git", "message"));
                return Programs.Attempt("commitcheck.py", new() { ["FORBIDDEN_NAMES"] = MadeUpName }, repo, "HEAD");
            }

            var (module, printed) = Commit(Path.Combine("Modules", "Sample"));
            var (tools, toolsPrinted) = Commit(Path.Combine("Modules", "Tools"));
            var (other, otherPrinted) = Commit(Path.Combine("Other", "Sample"));

            Assert.True(module == 1, printed);
            Assert.Contains("FAIL README.md:3: names an engine, \"BevyCSharp\", which a module names nowhere but in its assembly's name", printed);
            Assert.DoesNotContain("Sample.csproj", printed);
            Assert.DoesNotContain("REVIEW.md", printed);
            Assert.True(tools == 0, toolsPrinted);
            Assert.True(other == 0, otherPrinted);
        }
        finally
        {
            Delete(parent);
        }
    }

    [NeedsPythonFact]
    public void A_Message_Without_The_Three_Marks_Fails()
    {
        var (code, printed) = Check(new() { ["a.txt"] = "a\n" }, "Add a file.\n\nA file holds a letter.\n");

        Assert.Equal(1, code);
        Assert.Contains("FAIL the message does not begin with the three marks and a blank line", printed);
    }

    [NeedsPythonFact]
    public void A_Sentence_Without_Its_Full_Stop_Fails()
    {
        var (code, printed) = Check(new() { ["a.txt"] = "a\n" }, Lead + "A file holds a letter\n");

        Assert.Equal(1, code);
        Assert.Contains("FAIL the sentence does not end with a full stop", printed);
    }

    [NeedsPythonFact]
    public void A_Line_After_The_Sentence_Fails()
    {
        var (code, printed) = Check(new() { ["a.txt"] = "a\n" }, Lead + "A file holds a letter.\n\nSigned-off-by: someone\n");

        Assert.Equal(1, code);
        Assert.Contains("FAIL the message's description holds 3 lines, one sentence expected", printed);
    }

    [NeedsPythonFact]
    public void A_Comment_Line_Past_100_Columns_Fails_And_An_Address_Alone_Does_Not()
    {
        var comment = "# " + string.Join(' ', Enumerable.Repeat("word", 22)) + "\n";
        var address = "# https://example.com/" + new string('a', 100) + "\n";
        var (code, printed) = Check(new() { ["scripts/a.py"] = comment + address + "print(1)\n" }, Lead + "A script prints one.\n");

        Assert.Equal(1, code);
        Assert.Contains($"FAIL scripts/a.py:1: a comment line of {comment.Length - 1} columns, past 100", printed);
        Assert.DoesNotContain("scripts/a.py:2", printed);
    }

    [NeedsPythonFact]
    public void A_Long_Case_Arm_Of_A_Shell_Script_Is_No_Comment()
    {
        var arm = "    *) echo \"" + string.Join(' ', Enumerable.Repeat("word", 20)) + "\" >&2; exit 1 ;;\n";
        var script = "case \"$1\" in\n    a) echo a ;;\n" + arm + "esac\n";
        var (exit, printed) = Check(new() { ["build/fetch.sh"] = script }, Lead + "A script answers its argument.\n");

        Assert.True(exit == 0, printed);
    }

    [NeedsPythonFact]
    public void A_Commit_That_Takes_A_Submodule_Is_Not_Read_For_The_Submodules_Files()
    {
        var repo = Directory.CreateTempSubdirectory("commitcheck-").FullName;
        try
        {
            // The submodule's own long comment and dash are its checkout's to read, and the commit
            // that takes it holds the commit it points at alone.
            var nested = Path.Combine(repo, "modules", "Tools");
            Directory.CreateDirectory(nested);
            Git(nested, "init", "-q");
            File.WriteAllText(Path.Combine(nested, "a.py"), "# " + string.Join(' ', Enumerable.Repeat("word", 30)) + " \u2014 a dash\n");
            Git(nested, "add", "-A");
            Git(nested, "commit", "-q", "-m", "The nested commit.");
            Git(repo, "init", "-q");
            File.WriteAllText(Path.Combine(repo, ".gitmodules"), "[submodule \"modules/Tools\"]\n\tpath = modules/Tools\n\turl = https://example.com/Tools.git\n");
            Git(repo, "add", "-A");
            File.WriteAllText(Path.Combine(repo, ".git", "message"), Lead + "The module takes Tools.\n");
            Git(repo, "commit", "-q", "--cleanup=verbatim", "-F", Path.Combine(repo, ".git", "message"));

            var (exit, printed) = Programs.Attempt("commitcheck.py", new() { ["FORBIDDEN_NAMES"] = MadeUpName }, repo, "HEAD", "--thumbs=" + Path.Combine(repo, ".git", "thumbs"));

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
    public void A_Dash_Found_Is_Printed_Whole_Whatever_The_Systems_Encoding()
    {
        var (exit, printed) = Check(new() { ["notes.md"] = "A line \u2014 with a dash.\n" }, Lead + "A note holds a line.\n", environment: Programs.SingleByteLocale());

        Assert.Equal(1, exit);
        Assert.Contains("  FAIL notes.md:1: em dash, \"\u2014\"", Programs.Lines(printed));
    }

    [NeedsPythonFact]
    public void A_Markdown_Line_Is_Held_To_No_Width_Whatever_Its_Mark()
    {
        var bullet = "* " + string.Join(' ', Enumerable.Repeat("word", 25)) + "\n";
        var heading = "# " + string.Join(' ', Enumerable.Repeat("word", 25)) + "\n";
        var (exit, printed) = Check(new() { ["README.md"] = heading + "\n" + bullet }, Lead + "The README lists words.\n");

        Assert.True(exit == 0, printed);
    }

    [NeedsPythonFact]
    public void A_Long_Line_Of_Code_That_Names_An_Xml_Comments_Mark_Is_No_Comment()
    {
        var code = "var opened = text.StartsWith(\"<!--\", StringComparison.Ordinal);\n";
        var after = "var closed = text.Contains(\"-->\", StringComparison.Ordinal) && text.Length > 100 && opened && closed;\n";
        var (exit, printed) = Check(new() { ["A.cs"] = code + after }, Lead + "A file reads a mark.\n");

        Assert.True(exit == 0, printed);
    }

    [NeedsPythonFact]
    public void A_Name_Given_In_The_Environment_Fails_Where_A_Commit_Carries_It()
    {
        var files = new Dictionary<string, string> { ["scripts/a.py"] = $"# Written for {MadeUpName}.\nprint(1)\n" };

        var (code, printed) = Check(files, Lead + "A script prints one.\n");
        Assert.Equal(1, code);
        Assert.Contains("FAIL 1 hits of a forbidden name", printed);
        Assert.DoesNotContain(MadeUpName, printed);

        var (unset, warned) = Check(files, Lead + "A script prints one.\n", names: null);
        Assert.Equal(0, unset);
        Assert.Contains("WARN FORBIDDEN_NAMES is not set, so the names were not read for", warned);
    }

    [NeedsPillowFact]
    public void A_Picture_On_A_Dark_Ground_Fails()
    {
        var (code, printed) = Check(new() { [".github/assets/readings/2026-11-18-item-1-dark.webp"] = "black" }, Lead + "A dark picture is kept.\n");

        Assert.Equal(1, code);
        Assert.Contains("FAIL .github/assets/readings/2026-11-18-item-1-dark.webp: the ground is not white", printed);
    }

    // A repository of one commit holding the files given, a picture's text naming its color, and
    // the check run over it with the made-up name given or none.
    private static (int Code, string Printed) Check(Dictionary<string, string> files, string message, string? names = MadeUpName, Dictionary<string, string?>? environment = null)
    {
        var repo = Directory.CreateTempSubdirectory("commitcheck-").FullName;
        try
        {
            Git(repo, "init", "-q");
            foreach (var (path, text) in files)
            {
                var file = Path.Combine(repo, path);
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                if (path.EndsWith(".webp", StringComparison.Ordinal))
                    Programs.Run(Programs.Python.Value!, repo, "-I", "-c", $"from PIL import Image; Image.new('RGB', (64, 32), '{text}').save(r'{file}')");
                else
                    File.WriteAllText(file, text);
            }

            Git(repo, "add", "-A");
            File.WriteAllText(Path.Combine(repo, ".git", "message"), message);
            Git(repo, "commit", "-q", "--cleanup=verbatim", "-F", Path.Combine(repo, ".git", "message"));
            return Programs.Attempt("commitcheck.py", new(environment ?? []) { ["FORBIDDEN_NAMES"] = names }, repo, "HEAD", "--thumbs=" + Path.Combine(repo, ".git", "thumbs"));
        }
        finally
        {
            // Git writes its objects read-only, which Windows will not delete as they are.
            foreach (var file in Directory.EnumerateFiles(repo, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(repo, true);
        }
    }

    // The repository's identity, which the check reads as the only one a commit may carry, given on
    // the command line with signing off, so a commit is made alike in a workflow and on a desktop.
    private static string Git(string repo, params string[] arguments) =>
        Programs.Run("git", repo, ["-c", "user.name=EggyStudio", "-c", "user.email=owner@example.com", "-c", "commit.gpgsign=false", "-c", "core.autocrlf=false", .. arguments]);
}
