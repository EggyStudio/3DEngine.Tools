using Xunit;

namespace Tools.Tests;

public class HooksTests
{
    private const string Lead = "‎ ‎ ‎\n\n";

    [NeedsPythonFact]
    public void An_Installed_Hook_Prints_The_Checks_Lines_For_Each_Commit_And_Stops_None()
    {
        using var repo = new Repo();
        Assert.Contains("installed ", repo.Hooks("install"));

        var (bad, failed) = repo.Commit("a.txt", "A file holds a letter\n");
        var (good, passed) = repo.Commit("b.txt", Lead + "A second file holds a letter.\n");

        Assert.Equal(0, bad);
        Assert.Contains("FAIL the sentence does not end with a full stop", failed);
        Assert.Equal(0, good);
        Assert.Contains("WARN  A second file holds a letter.", passed);
        Assert.Equal("2", repo.Git("rev-list", "--count", "HEAD").Trim());
    }

    [NeedsPythonFact]
    public void A_Second_Install_Changes_Nothing_And_Remove_Takes_The_Hook_Out()
    {
        using var repo = new Repo();
        repo.Hooks("install");
        var installed = File.ReadAllText(repo.Hook);

        Assert.Contains("stands as installed", repo.Hooks("install"));
        Assert.Equal(installed, File.ReadAllText(repo.Hook));
        Assert.Contains("removed ", repo.Hooks("remove"));
        Assert.False(File.Exists(repo.Hook));
        Assert.DoesNotContain("FAIL", repo.Commit("a.txt", "No full stop\n").Output);
    }

    [NeedsPythonFact]
    public void Another_Repository_And_Another_Hook_Are_Refused_And_Left_As_They_Are()
    {
        using var repo = new Repo();
        var elsewhere = Directory.CreateTempSubdirectory("hooks-elsewhere-").FullName;
        try
        {
            var (fromElsewhere, refused) = Programs.Execute(Programs.Python.Value!, elsewhere, [], "-I", Repo.Script, "install", repo.Root);
            Assert.Equal(1, fromElsewhere);
            Assert.Contains("is not the repository the working folder lies in", refused);
            Assert.False(File.Exists(repo.Hook));
        }
        finally
        {
            Directory.Delete(elsewhere, true);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(repo.Hook)!);
        File.WriteAllText(repo.Hook, "#!/bin/sh\necho another's\n");
        Assert.Contains("a post-commit hook of another's", repo.Hooks("install", expected: 1));
        Assert.Contains("a post-commit hook of another's", repo.Hooks("remove", expected: 1));
        Assert.Equal("#!/bin/sh\necho another's\n", File.ReadAllText(repo.Hook));
    }

    [NeedsPythonFact]
    public void A_Hook_Whose_Check_Is_Gone_Prints_Nothing_And_The_Commit_Is_Made()
    {
        using var repo = new Repo();
        var scripts = Path.Combine(repo.Root, "..", Path.GetFileName(repo.Root) + "-scripts");
        Directory.CreateDirectory(scripts);
        foreach (var name in new[] { "hooks.py", "commitcheck.py" })
            File.Copy(Path.Combine(AppContext.BaseDirectory, "scripts", name), Path.Combine(scripts, name));
        try
        {
            var (code, printed) = Programs.Execute(Programs.Python.Value!, repo.Root, [], "-I", Path.Combine(scripts, "hooks.py"), "install");
            Assert.True(code == 0, printed);
            File.Delete(Path.Combine(scripts, "commitcheck.py"));

            var (made, output) = repo.Commit("a.txt", "No full stop\n");

            Assert.Equal(0, made);
            Assert.DoesNotContain("FAIL", output);
            Assert.Equal("1", repo.Git("rev-list", "--count", "HEAD").Trim());
        }
        finally
        {
            Directory.Delete(scripts, true);
        }
    }

    [NeedsPythonAndSymbolicLinksFact]
    public void A_Hook_With_No_Python_On_The_Path_Prints_Nothing_And_The_Commit_Is_Made()
    {
        using var repo = new Repo();
        repo.Hooks("install");
        // A path holding git alone, as a machine with no Python has it.
        var bin = Path.Combine(repo.Root, "..", Path.GetFileName(repo.Root) + "-bin");
        Directory.CreateDirectory(bin);
        var git = Programs.Run("sh", repo.Root, "-c", "command -v git").Trim();
        File.CreateSymbolicLink(Path.Combine(bin, "git"), git);
        try
        {
            var (made, output) = repo.Commit("a.txt", "No full stop\n", new() { ["PATH"] = bin });

            Assert.True(made == 0, output);
            Assert.DoesNotContain("FAIL", output);
        }
        finally
        {
            Directory.Delete(bin, true);
        }
    }

    // A made repository, its hook's path, and the script run from inside it, deleted with it.
    private sealed class Repo : IDisposable
    {
        public static readonly string Script = Path.Combine(AppContext.BaseDirectory, "scripts", "hooks.py");

        public string Root { get; } = Directory.CreateTempSubdirectory("hooks-").FullName;

        public string Hook => Path.Combine(Root, ".git", "hooks", "post-commit");

        public Repo() => Git("init", "-q");

        public string Git(params string[] arguments) =>
            Programs.Run("git", Root, ["-c", "user.name=EggyStudio", "-c", "user.email=owner@example.com", "-c", "commit.gpgsign=false", "-c", "core.autocrlf=false", .. arguments]);

        public string Hooks(string action, int expected = 0)
        {
            var (code, printed) = Programs.Execute(Programs.Python.Value!, Root, [], "-I", Script, action);
            Assert.True(code == expected, printed);
            return printed;
        }

        public (int Code, string Output) Commit(string file, string message, Dictionary<string, string?>? environment = null)
        {
            File.WriteAllText(Path.Combine(Root, file), "a\n");
            Git("add", "-A");
            File.WriteAllText(Path.Combine(Root, ".git", "message"), message);
            return Programs.Execute("git", Root, new(environment ?? []) { ["FORBIDDEN_NAMES"] = null },
                "-c", "user.name=EggyStudio", "-c", "user.email=owner@example.com", "-c", "commit.gpgsign=false",
                "commit", "-q", "--cleanup=verbatim", "-F", Path.Combine(Root, ".git", "message"));
        }

        public void Dispose()
        {
            foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(Root, true);
        }
    }
}
