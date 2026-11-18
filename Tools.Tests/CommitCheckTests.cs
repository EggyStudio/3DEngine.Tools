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
    public void Another_Product_In_A_Modules_Text_Warns_Outside_A_License_Or_A_Citation()
    {
        var parent = Directory.CreateTempSubdirectory("commitcheck-").FullName;
        try
        {
            var repo = Path.Combine(parent, "Modules", "Sample");
            Directory.CreateDirectory(Path.Combine(repo, "Sample"));
            Git(repo, "init", "-q");
            File.WriteAllText(Path.Combine(repo, "README.md"), "# Sample\n\nThe sky as Unity lights it.\nThe air after HDRP's model (2019).\nSee https://example.com/godot for more.\n");
            File.WriteAllText(Path.Combine(repo, "LICENSE"), "Parts by Unity Technologies.\n");
            File.WriteAllText(Path.Combine(repo, "Sample", "Sky.cs"), "namespace Sample;\n// The haze as Unreal draws it.\npublic class Sky { }\n");
            Git(repo, "add", "-A");
            File.WriteAllText(Path.Combine(repo, ".git", "message"), Lead + "A module holds a sky.\n");
            Git(repo, "commit", "-q", "--cleanup=verbatim", "-F", Path.Combine(repo, ".git", "message"));

            var (code, printed) = Programs.Attempt("commitcheck.py", new() { ["FORBIDDEN_NAMES"] = MadeUpName }, repo, "HEAD");

            Assert.True(code == 0, printed);
            Assert.Contains("WARN README.md:3: names another product, \"Unity\", where a module's text stands on its own terms", printed);
            Assert.DoesNotContain("README.md:4", printed);
            Assert.DoesNotContain("README.md:5", printed);
            Assert.DoesNotContain("LICENSE", printed);
            Assert.Single(Programs.Lines(printed), line => line.Contains("Sample/Sky.cs:2", StringComparison.Ordinal));
        }
        finally
        {
            Delete(parent);
        }
    }

    [NeedsPythonFact]
    public void An_Added_Line_Writing_The_Cheatsheet_As_Two_Words_Fails_In_Any_Repository()
    {
        // The two words are joined here from parts, so this file holds them on no line.
        var split = "Cheat" + " sheet";
        var (code, printed) = Check(new() { ["Sample/A.cs"] = $"class A {{ string Title = \"{split}\"; }}\n" }, Lead + "A title is kept.\n");

        Assert.Equal(1, code);
        Assert.Contains($"FAIL Sample/A.cs:1: the cheatsheet written as two words, \"{split}\"", printed);
    }

    [NeedsPythonFact]
    public void A_Message_Naming_An_Engine_Fails_In_A_Module_And_Not_In_Tools()
    {
        var parent = Directory.CreateTempSubdirectory("commitcheck-").FullName;
        try
        {
            (int, string) Commit(string at, string sentence)
            {
                var repo = Path.Combine(parent, at);
                Directory.CreateDirectory(repo);
                Git(repo, "init", "-q");
                File.WriteAllText(Path.Combine(repo, "a.txt"), "a\n");
                Git(repo, "add", "-A");
                File.WriteAllText(Path.Combine(repo, ".git", "message"), Lead + sentence + "\n");
                Git(repo, "commit", "-q", "--cleanup=verbatim", "-F", Path.Combine(repo, ".git", "message"));
                return Programs.Attempt("commitcheck.py", new() { ["FORBIDDEN_NAMES"] = MadeUpName }, repo, "HEAD");
            }

            // The engine's name is joined from parts, so this file holds it on no line of a module.
            var named = "A file follows the mend Bevy" + "CSharp made.";
            var (module, printed) = Commit(Path.Combine("Modules", "Sample"), named);
            var (assembly, kept) = Commit(Path.Combine("Modules", "Other"), "The assembly is named 3DEngine.Other.");
            var (tools, toolsPrinted) = Commit(Path.Combine("Modules", "Tools"), named);

            Assert.True(module == 1, printed);
            Assert.Contains("FAIL the message names an engine, \"Bevy" + "CSharp\", which a module names nowhere but in its assembly's name", printed);
            Assert.True(assembly == 0, kept);
            Assert.True(tools == 0, toolsPrinted);
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
    public void A_Picture_Added_Without_Its_Caption_Fails_Naming_The_Line_That_Would_Satisfy_It_In_Either_Folder()
    {
        foreach (var folder in new[] { ".github/assets/readings", ".github/devlog" })
        {
            var (code, printed) = Check(new() { [$"{folder}/2026-11-18-item-1-room.webp"] = "white" }, Lead + "A room is read.\n");

            Assert.Equal(1, code);
            Assert.Contains($"FAIL {folder}/2026-11-18-item-1-room.webp: no caption, which {folder}/captions.md gives as the line \"2026-11-18-item-1-room.webp | <one sentence>.\"", printed);
        }
    }

    [NeedsPillowFact]
    public void A_Picture_With_Its_Caption_Passes_And_A_Caption_Out_Of_Form_Fails()
    {
        var picture = ".github/assets/readings/2026-11-18-item-1-room.webp";
        var (code, printed) = Check(new() { [picture] = "white", [".github/assets/readings/captions.md"] = "2026-11-18-item-1-room.webp | The room at noon.\n" }, Lead + "A room is read.\n");
        var (unstopped, said) = Check(new() { [picture] = "white", [".github/assets/readings/captions.md"] = "2026-11-18-item-1-room.webp | The room at noon\n" }, Lead + "A room is read.\n");

        Assert.DoesNotContain("caption", printed);
        Assert.DoesNotContain("FAIL", printed);
        Assert.Equal(1, unstopped);
        Assert.Contains($"FAIL {picture}: its caption is not one sentence ending in a full stop, \"The room at noon\"", said);
    }

    [NeedsPillowFact]
    public void A_Picture_On_A_Dark_Ground_Fails()
    {
        var (code, printed) = Check(new() { [".github/assets/readings/2026-11-18-item-1-dark.webp"] = "black" }, Lead + "A dark picture is kept.\n");

        Assert.Equal(1, code);
        Assert.Contains("FAIL .github/assets/readings/2026-11-18-item-1-dark.webp: the ground is not white", printed);
    }

    [NeedsPythonFact]
    public void Staged_Files_And_A_Message_File_Are_Read_Before_The_Commit_And_The_Work_Tree_Is_Not()
    {
        var repo = Staged();
        try
        {
            var comment = "# " + string.Join(' ', Enumerable.Repeat("word", 22)) + "\n";
            File.WriteAllText(Path.Combine(repo, "b.py"), comment + "print(2)\n");
            Git(repo, "add", "b.py");
            // A change the work tree holds and the index does not, which the commit leaves out.
            File.WriteAllText(Path.Combine(repo, "a.py"), comment + "print(1)\n");
            var message = Path.Combine(repo, ".git", "next");
            File.WriteAllText(message, "A plain subject\n\nA script prints two.\n");

            var (code, printed) = Programs.Attempt("commitcheck.py", new() { ["FORBIDDEN_NAMES"] = MadeUpName }, repo, "--staged", "--message=" + message);

            Assert.Equal(1, code);
            Assert.Contains("staged   FAIL  A script prints two.", printed);
            Assert.Contains($"FAIL b.py:1: a comment line of {comment.Length - 1} columns, past 100", printed);
            Assert.Contains("FAIL the message does not begin with the three marks and a blank line", printed);
            Assert.DoesNotContain("a.py:1", printed);
        }
        finally
        {
            Delete(repo);
        }
    }

    [NeedsPythonFact]
    public void Staged_Without_A_Message_Reads_The_Files_And_Says_The_Message_Was_Not_Read_And_Nothing_Staged_Is_Said()
    {
        var repo = Staged();
        try
        {
            var (none, nothing) = Programs.Attempt("commitcheck.py", new() { ["FORBIDDEN_NAMES"] = MadeUpName }, repo, "--staged");
            File.WriteAllText(Path.Combine(repo, "b.py"), "# Prints two.\nprint(2)\n");
            Git(repo, "add", "b.py");

            var (code, printed) = Programs.Attempt("commitcheck.py", new() { ["FORBIDDEN_NAMES"] = MadeUpName }, repo, "--staged");

            Assert.True(none == 0, nothing);
            Assert.Contains("nothing is staged", nothing);
            Assert.True(code == 0, printed);
            Assert.Contains("staged   ok  (no message given)", printed);
            Assert.Contains("note no message is given with --message, so the message was not read", printed);
        }
        finally
        {
            Delete(repo);
        }
    }

    [NeedsPillowFact]
    public void A_Staged_Picture_On_A_Dark_Ground_Fails_Read_From_The_Index()
    {
        var repo = Staged();
        try
        {
            var file = Path.Combine(repo, ".github", "assets", "readings", "2026-11-18-dark.webp");
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            Programs.Run(Programs.Python.Value!, repo, "-I", "-c", $"from PIL import Image; Image.new('RGB', (64, 32), 'black').save(r'{file}')");
            Git(repo, "add", "-A");
            // The work tree's picture is made white after it is staged, so a dark ground is read
            // from the index alone.
            Programs.Run(Programs.Python.Value!, repo, "-I", "-c", $"from PIL import Image; Image.new('RGB', (64, 32), 'white').save(r'{file}')");

            var (code, printed) = Programs.Attempt("commitcheck.py", new() { ["FORBIDDEN_NAMES"] = MadeUpName }, repo, "--staged", "--thumbs=" + Path.Combine(repo, ".git", "thumbs"));

            Assert.Equal(1, code);
            Assert.Contains("FAIL .github/assets/readings/2026-11-18-dark.webp: ", printed);
        }
        finally
        {
            Delete(repo);
        }
    }

    // A repository of one commit in the form, its identity set in its own configuration, which the
    // check reads for a commit not yet made.
    private static string Staged()
    {
        var repo = Directory.CreateTempSubdirectory("commitcheck-").FullName;
        Git(repo, "init", "-q");
        Git(repo, "config", "user.name", "EggyStudio");
        Git(repo, "config", "user.email", "owner@example.com");
        Git(repo, "config", "core.autocrlf", "false");
        Commit(repo, "a.py", Lead + "A script prints one.\n");
        return repo;
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
