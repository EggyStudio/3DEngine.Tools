using Xunit;

namespace Tools.Tests;

public class RewrapCheckTests
{
    private static readonly string Words = string.Join(' ', Enumerable.Repeat("word", 30));

    [NeedsPythonFact]
    public void A_Copy_Is_Rewrapped_With_Its_Code_And_Words_The_Same_And_A_Second_Run_Changing_Nothing()
    {
        using var folders = new Folders();
        var repo = folders.Repository(new()
        {
            ["A.cs"] = $"class A\n{{\n    // {Words}\n    int x = 1;\n}}\n",
            ["b.py"] = $"# {Words}\n# {Words}\nprint(1)\n",
            ["c.csproj"] = $"<Project>\n    <!-- {Words} -->\n</Project>\n",
            ["notes.txt"] = $"# {Words}\n",
        });
        var original = File.ReadAllText(Path.Combine(repo, "A.cs"));

        var (exit, printed) = Programs.Attempt("rewrapcheck.py", [], folders.Work, repo);

        Assert.True(exit == 0, printed);
        Assert.Contains("repo: 3 files copied, 3 rewrapped with ", printed);
        Assert.Contains("  the second run: 0 comment lines past 100 columns in 0 files rewrapped", printed);
        Assert.DoesNotContain("FAIL", printed);
        Assert.Equal(original, File.ReadAllText(Path.Combine(repo, "A.cs")));
        Assert.NotEqual(original, File.ReadAllText(Path.Combine(folders.Work, "repo", "A.cs")));
    }

    [NeedsPythonFact]
    public void A_Rewrap_That_Changes_Code_Past_Its_Own_Guard_Is_Found()
    {
        // A copy of the scripts whose rewrap changes a line of code and whose own check of the code
        // is switched off, which the proof reads by its own rule of what a comment is.
        using var folders = new Folders();
        var scripts = Path.Combine(folders.Root, "scripts");
        Directory.CreateDirectory(scripts);
        foreach (var name in new[] { "commitcheck.py", "rewrapcheck.py" })
            File.Copy(Path.Combine(AppContext.BaseDirectory, "scripts", name), Path.Combine(scripts, name));
        var rewrap = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scripts", "rewrap.py"));
        foreach (var part in new[] { "        if not found:\n            out.append(lines[i])", "    if code != code_after:" })
            Assert.Contains(part, rewrap);
        rewrap = rewrap.Replace("        if not found:\n            out.append(lines[i])", "        if not found:\n            out.append(lines[i].replace(\"print\", \"prant\"))", StringComparison.Ordinal)
            .Replace("    if code != code_after:", "    if False:", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(scripts, "rewrap.py"), rewrap);
        var repo = folders.Repository(new() { ["b.py"] = $"# {Words}\nprint(1)\n" });

        var (exit, printed) = Programs.Attempt(Path.Combine(scripts, "rewrapcheck.py"), [], folders.Work, repo);

        Assert.Equal(1, exit);
        Assert.Contains("  FAIL b.py: its code is not byte-equal after the rewrap", printed);
        Assert.Equal($"# {Words}\nprint(1)\n", File.ReadAllText(Path.Combine(repo, "b.py")));
    }

    [NeedsPythonFact]
    public void A_Work_Folder_Inside_A_Repository_Is_Refused()
    {
        using var folders = new Folders();
        var repo = folders.Repository(new() { ["b.py"] = "print(1)\n" });

        var (exit, printed) = Programs.Attempt("rewrapcheck.py", [], Path.Combine(repo, "work"), repo);

        Assert.Equal(1, exit);
        Assert.Contains("lies inside", printed);
        Assert.False(Directory.Exists(Path.Combine(repo, "work")));
    }

    // A folder holding a made repository and a work folder beside it, deleted with both.
    private sealed class Folders : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("rewrapcheck-").FullName;

        public string Work => Path.Combine(Root, "work");

        public string Repository(Dictionary<string, string> files)
        {
            var repo = Path.Combine(Root, "repo");
            Directory.CreateDirectory(repo);
            Programs.Run("git", repo, "init", "-q");
            foreach (var (path, text) in files) File.WriteAllText(Path.Combine(repo, path), text);
            Programs.Run("git", repo, "-c", "core.autocrlf=false", "add", "-A");
            return repo;
        }

        public void Dispose()
        {
            foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(Root, true);
        }
    }
}
