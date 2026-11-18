using Xunit;

namespace Tools.Tests;

/// <summary>
/// The sheets folder holds sheet.py and chart.py with their tests, examples and page, standing on
/// its own, so a copy of the folder alone is a repository of them; Tools runs the folder's tests
/// here, where the build runs them on three systems.
/// </summary>
public class SheetsTests
{
    private static readonly string Folder = Path.Combine(Programs.Root(), "sheets");

    // What a file of the folder would name to reach outside it, a parent, Tools' own folders or its
    // name, any of which a copy of the folder alone would not hold.
    private static readonly string[] Outside = ["../", "..\\", "scripts/", ".github/", "Tools"];

    [NeedsPythonFact]
    public void The_Folders_Own_Tests_Pass_In_It()
    {
        var (code, printed) = Tests(Folder);

        Assert.True(code == 0, printed);
        Assert.Contains("OK", printed);
    }

    // The folder is said to stand on its own on any system, which is shown only where its drawing
    // tests draw on each, so the workflow's three jobs read Pillow and skip none of them.
    [InBuildWorkflowFact]
    public void In_The_Build_Workflow_Pillow_Is_There_And_None_Of_The_Folders_Tests_Is_Skipped()
    {
        Assert.True(Programs.Pillow.Value, $"{Programs.Python.Value} has no Pillow, so the folder's drawing tests would be skipped.");

        var (code, printed) = Tests(Folder);

        Assert.True(code == 0, printed);
        Assert.DoesNotContain("skipped", printed);
    }

    [NeedsPythonFact]
    public void A_Copy_Of_The_Folder_Alone_Names_Nothing_Outside_It_And_Its_Tests_Pass_There()
    {
        var copy = Directory.CreateTempSubdirectory("sheets-").FullName;
        try
        {
            foreach (var file in Directory.EnumerateFiles(Folder, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(Folder, file);
                if (relative.Split(Path.DirectorySeparatorChar).Contains("__pycache__")) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(copy, relative))!);
                File.Copy(file, Path.Combine(copy, relative));
                if (file.EndsWith(".webp", StringComparison.Ordinal) || file.EndsWith(".png", StringComparison.Ordinal)) continue;
                var text = File.ReadAllText(file);
                foreach (var name in Outside)
                    Assert.False(text.Contains(name, StringComparison.Ordinal), $"{relative} names {name}, outside the folder");
            }

            var (code, printed) = Tests(copy);

            Assert.True(code == 0, printed);
            Assert.Contains("OK", printed);
        }
        finally
        {
            Directory.Delete(copy, true);
        }
    }

    // The folder's tests run as its page says, bytecode left unwritten, from the folder's root.
    private static (int Code, string Printed) Tests(string folder) =>
        Programs.Execute(Programs.Python.Value!, folder, [], "-I", "-B", "-m", "unittest", "discover", "-s", "tests", "-t", ".");
}
