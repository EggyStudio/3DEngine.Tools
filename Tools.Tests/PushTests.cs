using System.Text.Json;
using Xunit;

namespace Tools.Tests;

public class PushTests
{
    private const string Lead = "‎ ‎ ‎\n\n";

    private static object Run(long id, string status, string? conclusion = null) =>
        new { databaseId = id, status, conclusion, url = $"https://github.com/Sample/Repo/actions/runs/{id}" };

    [NeedsPythonFact]
    public void A_Head_Whose_Run_Passed_Is_Pushed_And_The_Remote_Head_Printed()
    {
        using var world = new World();
        var head = world.Commit("b.py", "A script prints two.");

        var (code, printed) = world.Push(head, new() { [world.Base] = Run(1, "completed", "success") });

        Assert.True(code == 0, printed);
        Assert.Equal(head, world.RemoteHead());
        Assert.Contains($"remote main now {head[..7]}", printed);
    }

    [NeedsPythonFact]
    public void A_Dry_Run_Does_All_But_The_Push()
    {
        using var world = new World();
        var head = world.Commit("b.py", "A script prints two.");

        var (code, printed) = world.Push(head, new() { [world.Base] = Run(1, "completed", "success") }, "--dry-run");

        Assert.True(code == 0, printed);
        Assert.Equal(world.Base, world.RemoteHead());
        Assert.Contains("dry run, nothing pushed", printed);
    }

    [NeedsPythonFact]
    public void A_Commit_That_Fails_The_Check_Stops_The_Push_With_Exit_2_And_Its_Lines()
    {
        using var world = new World();
        var head = world.CommitMessage("b.py", "A plain subject\n\nA script prints two.\n");

        var (code, printed) = world.Push(head, new() { [world.Base] = Run(1, "completed", "success") });

        Assert.Equal(2, code);
        Assert.Contains("FAIL the message does not begin with the three marks", printed);
        Assert.Equal(world.Base, world.RemoteHead());
    }

    [NeedsPythonFact]
    public void A_Run_Still_Going_Is_Waited_On_And_Past_The_Bound_The_Wait_Ends_With_Exit_3()
    {
        using var world = new World();
        var head = world.Commit("b.py", "A script prints two.");

        var (code, printed) = world.Push(head, new() { [world.Base] = new object[] { Run(1, "queued"), Run(1, "in_progress"), Run(1, "completed", "success") } });
        Assert.True(code == 0, printed);
        Assert.Contains("waits for run 1", printed);
        Assert.Equal(head, world.RemoteHead());

        var second = world.Commit("c.py", "A script prints three.");
        var (late, said) = world.Push(second, new() { [head] = Run(2, "in_progress") }, "--wait-minutes=0.01");
        Assert.Equal(3, late);
        Assert.Contains("still in progress", said);
        Assert.Equal(head, world.RemoteHead());
    }

    [NeedsPythonFact]
    public void A_Head_With_No_Run_Takes_The_State_Of_The_Newest_Commit_Back_That_Has_One()
    {
        using var world = new World();
        var middle = world.Commit("b.py", "A script prints two.");
        world.Push(middle, new() { [world.Base] = Run(1, "completed", "success") });
        var runless = world.Commit("c.py", "A script prints three.");
        world.Push(runless, new() { [middle] = Run(2, "completed", "success") });
        var next = world.Commit("d.py", "A script prints four.");

        // The remote head, runless, has none, and the commit before it has a run still going, which
        // is waited on and then read, where waiting on the head's own would never end.
        var (code, printed) = world.Push(next, new() { [middle] = new object[] { Run(2, "in_progress"), Run(2, "completed", "success") } });

        Assert.True(code == 0, printed);
        Assert.Contains($"{runless[..7]} has no run, the newest back with one is {middle[..7]}", printed);
        Assert.Equal(next, world.RemoteHead());
    }

    [NeedsPythonFact]
    public void A_Failed_Run_Stops_The_Push_With_Exit_4_And_Its_Address_Unless_Over_Red_Is_Given()
    {
        using var world = new World();
        var head = world.Commit("b.py", "A script prints two.");
        var runs = new Dictionary<string, object?> { [world.Base] = Run(7, "completed", "failure") };

        var (code, printed) = world.Push(head, runs);
        Assert.Equal(4, code);
        Assert.Contains("https://github.com/Sample/Repo/actions/runs/7", printed);
        Assert.Equal(world.Base, world.RemoteHead());

        var (over, said) = world.Push(head, runs, "--over-red");
        Assert.True(over == 0, said);
        Assert.Equal(head, world.RemoteHead());
    }

    [NeedsPythonFact]
    public void A_Remote_That_Moved_Is_Exit_5_With_Gits_Line_And_The_Remote_Is_Left_As_It_Stood()
    {
        using var world = new World();
        var other = world.MoveRemote("z.py");
        var head = world.Commit("b.py", "A script prints two.");

        var (code, printed) = world.Push(head, new() { [other] = Run(1, "completed", "success") });

        Assert.Equal(5, code);
        Assert.Contains("rejected", printed);
        Assert.Equal(other, world.RemoteHead());
        Assert.DoesNotContain("--force", File.ReadAllText(Programs.ScriptPath("push.py")));
    }

    [NeedsPythonFact]
    public void A_Run_That_Could_Not_Be_Read_Is_Exit_6_And_Nothing_Is_Pushed()
    {
        using var world = new World();
        var head = world.Commit("b.py", "A script prints two.");

        var (code, printed) = world.Push(head, null);

        Assert.Equal(6, code);
        Assert.Contains("gh run list failed", printed);
        Assert.Equal(world.Base, world.RemoteHead());
    }

    [NeedsPythonAndFlockFact]
    public void The_Run_Is_Asked_For_By_The_Build_Workflow_Unless_Another_Or_None_Is_Named()
    {
        using var world = new World();
        var head = world.Commit("b.py", "A script prints two.");
        // A gh that writes the arguments it is given, a line each call, and finds no run.
        var bin = Path.Combine(world.Root, "bin");
        Directory.CreateDirectory(bin);
        var calls = Path.Combine(world.Root, "calls.txt");
        var gh = Path.Combine(bin, "gh");
        File.WriteAllText(gh, $"#!/bin/sh\necho \"$*\" >> '{calls}'\necho '[]'\n");
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(gh, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        Dictionary<string, string?> path = new() { ["PATH"] = bin + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH") };

        string[] Calls(params string[] options)
        {
            File.Delete(calls);
            var (code, printed) = Programs.Attempt("push.py", path, [world.Work, "Sample/Repo", head, "--dry-run", .. options]);
            Assert.True(code == 0, printed);
            return File.ReadAllLines(calls);
        }

        Assert.All(Calls(), call => Assert.Contains("-R Sample/Repo --commit " + world.Base + " -L 1 -w build --json", call));
        Assert.All(Calls("--workflow", "pack"), call => Assert.Contains(" -w pack --json", call));
        Assert.All(Calls("--workflow="), call => Assert.DoesNotContain(" -w ", call));
    }

    // A bare repository as origin, a clone of it that holds the commit pushed, and the run reader's
    // answers, the runs of each commit by its hash, a list of them as the run changes by each ask.
    private sealed class World : IDisposable
    {
        public string Root { get; } = Directory.CreateTempSubdirectory("push-").FullName;
        public string Work => Path.Combine(Root, "work");
        public string Origin => Path.Combine(Root, "origin.git");
        public string Base { get; }

        public World()
        {
            Git(Root, "init", "-q", "--bare", "-b", "main", Origin);
            Git(Root, "clone", "-q", Origin, Work);
            Git(Work, "checkout", "-q", "-b", "main");
            Base = Commit("a.py", "A script prints one.");
            Git(Work, "push", "-q", "origin", "main");
        }

        public string Git(string repo, params string[] arguments) =>
            Programs.Run("git", repo, ["-c", "user.name=EggyStudio", "-c", "user.email=owner@example.com", "-c", "commit.gpgsign=false", "-c", "core.autocrlf=false", .. arguments]);

        public string Commit(string file, string sentence) => CommitMessage(file, Lead + sentence + "\n");

        public string CommitMessage(string file, string message) => CommitIn(Work, file, message);

        private string CommitIn(string repo, string file, string message)
        {
            File.WriteAllText(Path.Combine(repo, file), "# Prints a number.\nprint(1)\n");
            Git(repo, "add", file);
            var path = Path.Combine(repo, ".git", "message");
            File.WriteAllText(path, message);
            Git(repo, "commit", "-q", "--cleanup=verbatim", "-F", path);
            return Git(repo, "rev-parse", "HEAD").Trim();
        }

        // A commit another clone pushes to origin, so the work clone's push is not a fast-forward.
        public string MoveRemote(string file)
        {
            var clone = Path.Combine(Root, "other");
            Git(Root, "clone", "-q", Origin, clone);
            var hash = CommitIn(clone, file, Lead + "A script prints nine.\n");
            Git(clone, "push", "-q", "origin", "HEAD:main");
            return hash;
        }

        public string RemoteHead() => Git(Origin, "rev-parse", "refs/heads/main").Trim();

        public (int Code, string Printed) Push(string hash, Dictionary<string, object?>? runs, params string[] options)
        {
            var answers = Path.Combine(Root, "answers.json");
            File.WriteAllText(answers, JsonSerializer.Serialize(runs is null ? new { failed = "no login" } : (object)new { runs }));
            return Programs.Attempt("push.py", new() { ["PUSH_ANSWERS"] = answers },
                [Work, "Sample/Repo", hash, "--poll-seconds=0.2", .. options]);
        }

        public void Dispose()
        {
            foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(Root, true);
        }
    }
}
