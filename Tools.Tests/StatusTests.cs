using System.Text.Json;
using Xunit;

namespace Tools.Tests;

public class StatusTests
{
    private const string Row = "[Devlog](https://github.com/EggyStudio/Sample/blob/main/DEVLOG.md) [Cheatsheet](https://github.com/EggyStudio/Sample/blob/main/CHEATSHEET.md)";

    [NeedsPythonFact]
    public void A_Repositorys_Line_Gives_Its_Heads_Its_Run_Its_Review_Its_Devlog_Its_Pin_And_Its_Row()
    {
        using var world = new World();
        var tools = world.Tools(commits: 2);
        var repo = world.Module("Sample", Row + "\n\n# Sample\n", tools, pinnedBack: 1);
        // A review past 90 KB, and a picture committed after the devlog was last written.
        world.Commit(repo, ".github/REVIEW.md", new string('a', 95 * 1024));
        world.Commit(repo, ".github/assets/readings/2026-11-18-item-1.webp", "picture");
        world.Commit(repo, "a.txt", "ahead\n", push: false);
        var remote = world.Git(repo, "rev-parse", "--short=7", "origin/main").Trim();
        var local = world.Git(repo, "rev-parse", "--short=7", "HEAD").Trim();

        var (code, printed) = world.Status(new() { ["EggyStudio/Sample"] = new { headSha = remote + "0000", status = "completed", conclusion = "success" } }, tools, repo);

        Assert.True(code == 0, printed);
        var line = Assert.Single(Programs.Lines(printed), l => l.StartsWith("Sample ", StringComparison.Ordinal));
        Assert.Equal($"Sample  local {local}  remote {remote}  ahead 1  run {remote} completed success  review 95 KB  devlog behind its newest picture  Tools at {world.ToolsAt(tools, 1)}, 1 behind  row ok", line);
    }

    [NeedsPythonFact]
    public void A_Readme_Without_The_Row_And_A_Repository_At_Its_Pin_Read_As_Such()
    {
        using var world = new World();
        var tools = world.Tools(commits: 1);
        var repo = world.Module("Sample", "# Sample\n", tools, pinnedBack: 0);

        var (code, printed) = world.Status(new() { ["EggyStudio/Sample"] = null }, tools, repo);

        Assert.True(code == 0, printed);
        Assert.Contains("  run none  ", printed);
        Assert.Contains("  Tools at its head  row missing", printed);
        Assert.DoesNotContain("review", printed);
        Assert.DoesNotContain("devlog behind", printed);
    }

    [NeedsPythonFact]
    public void Git_Or_Gh_Out_Of_Reach_Gives_Exit_Code_2_And_Says_Which()
    {
        using var world = new World();
        var tools = world.Tools(commits: 1);
        var repo = world.Module("Sample", Row + "\n\n# Sample\n", tools, pinnedBack: 0);
        var plain = Path.Combine(world.Root, "Plain");
        Directory.CreateDirectory(plain);

        var (gitCode, gitPrinted) = world.Status(new(), tools, plain);
        var (ghCode, ghPrinted) = world.Status(new() { ["EggyStudio/Sample"] = new { failed = "HTTP 503" } }, tools, repo);

        Assert.Equal(2, gitCode);
        Assert.Contains("Plain  git unreached, git rev-parse failed", gitPrinted);
        Assert.Equal(2, ghCode);
        Assert.Contains("  run unreached, gh run list failed, HTTP 503  ", ghPrinted);
    }

    // Made repositories, each with a bare remote its address in GitHub's form is sent to, and
    // a Tools repository its pin is read against, deleted with the folder.
    private sealed class World : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("status-").FullName;

        public string Git(string repo, params string[] arguments) =>
            Programs.Run("git", repo, ["-c", "user.name=EggyStudio", "-c", "user.email=owner@example.com", "-c", "commit.gpgsign=false", "-c", "core.autocrlf=false", "-c", "protocol.file.allow=always", .. arguments]);

        public string Tools(int commits)
        {
            var tools = Path.Combine(Root, "Tools");
            Directory.CreateDirectory(tools);
            Git(tools, "init", "-q", "-b", "main");
            for (int i = 0; i < commits; i++) Commit(tools, $"t{i}.txt", $"{i}\n", push: false);
            return tools;
        }

        public string ToolsAt(string tools, int back) => Git(tools, "rev-parse", "--short=7", $"HEAD~{back}").Trim();

        public string Module(string name, string readme, string tools, int pinnedBack)
        {
            var bare = Path.Combine(Root, name + ".git");
            Directory.CreateDirectory(bare);
            Git(bare, "init", "-q", "--bare", "-b", "main");
            var repo = Path.Combine(Root, name);
            Directory.CreateDirectory(repo);
            Git(repo, "init", "-q", "-b", "main");
            var address = $"https://github.com/EggyStudio/{name}.git";
            Git(repo, "remote", "add", "origin", address);
            Git(repo, "config", $"url.{bare}.insteadOf", address);
            // The module's own Tools, a clone at the pin, which git tracks as the commit it points
            // at, made with the line ends of every other git call here, where a clone with the
            // machine's would write CRLF that a later checkout reads as a change, as Git for
            // Windows does.
            Git(Root, "clone", "-q", tools, Path.Combine(repo, "modules", "Tools"));
            Git(Path.Combine(repo, "modules", "Tools"), "checkout", "-q", $"HEAD~{pinnedBack}");
            File.WriteAllText(Path.Combine(repo, "README.md"), readme);
            File.WriteAllText(Path.Combine(repo, "DEVLOG.md"), "# devlog\n");
            Git(repo, "add", "-A");
            Git(repo, "commit", "-q", "-m", "The module.");
            Git(repo, "push", "-q", "origin", "main");
            return repo;
        }

        public void Commit(string repo, string path, string text, bool push = true)
        {
            var file = Path.Combine(repo, path);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, text);
            Git(repo, "add", "-A");
            // A second apart by the committer's date, which the script compares, so a picture's
            // commit stands after the devlog's.
            var when = DateTimeOffset.UtcNow.AddSeconds(Clock++).ToString("o");
            var (code, printed) = Programs.Execute("git", repo, new() { ["GIT_COMMITTER_DATE"] = when, ["GIT_AUTHOR_DATE"] = when },
                "-c", "user.name=EggyStudio", "-c", "user.email=owner@example.com", "-c", "commit.gpgsign=false", "-c", "core.autocrlf=false",
                "commit", "-q", "-m", $"Adds {path}.");
            Assert.True(code == 0, printed);
            if (push) Git(repo, "push", "-q", "origin", "main");
        }

        private int Clock = 1;

        public (int Code, string Printed) Status(Dictionary<string, object?> runs, string tools, params string[] repos)
        {
            var answers = Path.Combine(Root, "answers.json");
            File.WriteAllText(answers, JsonSerializer.Serialize(runs));
            return Programs.Attempt("status.py", new() { ["STATUS_ANSWERS"] = answers }, ["--tools=" + tools, .. repos]);
        }

        public void Dispose()
        {
            foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(Root, true);
        }
    }
}
