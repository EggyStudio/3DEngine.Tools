using Xunit;

namespace Tools.Tests;

public class PinTests
{
    private const string Head = "| fact | the check it calls | in its form since | what changed it after |\n|---|---|---|---|\n";

    [NeedsPythonFact]
    public void A_Module_Moves_Its_Pin_Is_Told_Its_New_And_Changed_Facts_And_Has_Its_Devlog_Written_With_Nothing_Committed()
    {
        using var world = new World();
        var first = world.ToolsCommit(Head + "| `The_First` | `ModuleChecks.FirstFault` | `aaaaaaa` | |\n| `The_Third` | `ModuleChecks.ThirdFault` | `aaaaaaa` | |\n");
        // The third's row comes to name the pin the module stands at, a change it holds already.
        var second = world.ToolsCommit(Head + "| `The_First` | `ModuleChecks.FirstFault` | `aaaaaaa` | `bbbbbbb`, a mend |\n| `The_Second` | `ModuleChecks.SecondFault` | `bbbbbbb` | |\n"
            + $"| `The_Third` | `ModuleChecks.ThirdFault` | `aaaaaaa` | `{first[..7]}`, a mend the pin holds |\n");
        var module = world.Module(pinnedAt: first);
        var commits = world.Git(module, "rev-list", "--count", "HEAD").Trim();

        var (code, printed) = Programs.Execute(Programs.Python.Value!, module, [], "-I", Programs.ScriptPath("pin.py"), second);

        Assert.True(code == 0, printed);
        var lines = Programs.Lines(printed);
        Assert.Contains($"Tools moved from {first[..7]} to {second[..7]}, 1 commit", lines);
        Assert.Contains("  a new fact, The_Second, calling ModuleChecks.SecondFault, in its form since `bbbbbbb`", lines);
        Assert.Contains("  a changed fact, The_First, calling ModuleChecks.FirstFault, changed since the pin by `bbbbbbb`", lines);
        Assert.DoesNotContain(lines, line => line.Contains("The_Third", StringComparison.Ordinal));
        Assert.Contains("no CHEATSHEET.md is kept, so none is written", lines);
        Assert.Contains("DEVLOG.md written again by the new pin's devlog.py", lines);
        Assert.Equal(second, world.Git(Path.Combine(module, "modules", "Tools"), "rev-parse", "HEAD").Trim());
        Assert.Contains("Commits that day", File.ReadAllText(Path.Combine(module, "DEVLOG.md")));
        Assert.Equal(commits, world.Git(module, "rev-list", "--count", "HEAD").Trim());
        Assert.Contains(" M modules/Tools", world.Git(module, "status", "--porcelain"));
    }

    // Tools' old address, joined from parts so no line of this repository holds it.
    private const string Old = "https://github.com/EggyStudio/" + "Tools.git";

    [NeedsPythonFact]
    public void A_Submodule_Named_By_Tools_Old_Address_Is_Moved_To_Its_New_One()
    {
        using var world = new World();
        var first = world.ToolsCommit(Head + "| `The_First` | `ModuleChecks.FirstFault` | `aaaaaaa` | |\n");
        var module = world.Module(pinnedAt: first);
        File.WriteAllText(Path.Combine(module, ".gitmodules"), "[submodule \"modules/Tools\"]\n\tpath = modules/Tools\n\turl = " + Old + "\n");

        var (code, printed) = Programs.Execute(Programs.Python.Value!, module, [], "-I", Programs.ScriptPath("pin.py"), first);

        Assert.True(code == 0, printed);
        Assert.Contains("modules/Tools now named by https://github.com/EggyStudio/3DEngine.Tools.git, where " + Old + " named it", printed);
        Assert.Contains("url = https://github.com/EggyStudio/3DEngine.Tools.git", File.ReadAllText(Path.Combine(module, ".gitmodules")));
    }

    [NeedsPythonFact]
    public void A_Repository_With_No_Tools_And_A_Folder_Outside_Git_Are_Refused()
    {
        using var world = new World();
        var plain = Path.Combine(world.Root, "Plain");
        Directory.CreateDirectory(plain);
        world.Git(plain, "init", "-q");
        var outside = Directory.CreateTempSubdirectory("pin-outside-").FullName;
        try
        {
            var (noTools, said) = Programs.Execute(Programs.Python.Value!, plain, [], "-I", Programs.ScriptPath("pin.py"));
            var (noGit, told) = Programs.Execute(Programs.Python.Value!, outside, [], "-I", Programs.ScriptPath("pin.py"));

            Assert.Equal(2, noTools);
            Assert.Contains("holds no modules/Tools", said);
            Assert.Equal(2, noGit);
            Assert.Contains("git rev-parse --show-toplevel failed", told);
        }
        finally
        {
            Directory.Delete(outside, true);
        }
    }

    // A made Tools whose README holds the table of what a module carries and whose scripts hold the
    // devlog, and a module beside it whose modules/Tools is a clone of it at a pin.
    private sealed class World : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("pin-").FullName;

        private string Tools => Path.Combine(Root, "Tools");

        public string Git(string repo, params string[] arguments) =>
            Programs.Run("git", repo, ["-c", "user.name=EggyStudio", "-c", "user.email=owner@example.com", "-c", "commit.gpgsign=false", "-c", "core.autocrlf=false", "-c", "protocol.file.allow=always", .. arguments]);

        public string ToolsCommit(string table)
        {
            if (!Directory.Exists(Tools))
            {
                Directory.CreateDirectory(Path.Combine(Tools, "scripts"));
                Git(Tools, "init", "-q", "-b", "main");
                foreach (var script in new[] { "devlog.py", "commitcheck.py" })
                    File.Copy(Programs.ScriptPath(script), Path.Combine(Tools, "scripts", script));
            }
            File.WriteAllText(Path.Combine(Tools, "README.md"), "# Tools\n\n## Binding it in your engine\n\n### What a module carries\n\n" + table + "\n## Building\n");
            Git(Tools, "add", "-A");
            Git(Tools, "commit", "-q", "-m", "The table.");
            return Git(Tools, "rev-parse", "HEAD").Trim();
        }

        public string Module(string pinnedAt)
        {
            var module = Path.Combine(Root, "Sample");
            Directory.CreateDirectory(module);
            Git(module, "init", "-q", "-b", "main");
            Git(Root, "clone", "-q", Tools, Path.Combine(module, "modules", "Tools"));
            Git(Path.Combine(module, "modules", "Tools"), "checkout", "-q", pinnedAt);
            File.WriteAllText(Path.Combine(module, "README.md"), "# Sample\n");
            Git(module, "add", "-A");
            File.WriteAllText(Path.Combine(module, ".git", "message"), "‎ ‎ ‎\n\nThe module takes Tools.\n");
            Git(module, "commit", "-q", "--cleanup=verbatim", "-F", Path.Combine(module, ".git", "message"));
            return module;
        }

        public void Dispose()
        {
            foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(Root, true);
        }
    }
}
