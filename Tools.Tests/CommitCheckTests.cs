using Xunit;

namespace Tools.Tests;

public class CommitCheckTests
{
    private const string Lead = "‎ ‎ ‎\n\n";

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
    private static (int Code, string Printed) Check(Dictionary<string, string> files, string message, string? names = MadeUpName)
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
            return Programs.Attempt("commitcheck.py", new() { ["FORBIDDEN_NAMES"] = names }, repo, "HEAD", "--thumbs=" + Path.Combine(repo, ".git", "thumbs"));
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
