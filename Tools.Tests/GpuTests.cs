using System.Diagnostics;
using System.Text;
using Xunit;

namespace Tools.Tests;

/// <summary>
/// Skipped where no Python 3 runs or on Windows, which has no flock, so the GPU's gate is read on
/// Linux and macOS, the systems a GPU's programs are run on.
/// </summary>
public sealed class NeedsPythonAndFlockFactAttribute : FactAttribute
{
    public NeedsPythonAndFlockFactAttribute()
    {
        if (Programs.Python.Value is null) Skip = "No Python 3 runs here as python3 or python.";
        else if (OperatingSystem.IsWindows()) Skip = "Windows has no flock, which the gate takes.";
    }
}

public class GpuTests
{
    // A gate on a lock and a waiting folder of the test's own, so no test meets the machine's GPU.
    private sealed class Gate : IDisposable
    {
        public readonly string Folder = Directory.CreateTempSubdirectory("gpu-").FullName;
        public string Lock => Path.Combine(Folder, "gpu.lock");
        public string Waiting => Path.Combine(Folder, "gpu.waiting");
        public string Log => Path.Combine(Folder, "log.txt");
        private readonly List<Process> started = [];

        // A command that writes its word to the log as it starts and again as it ends, a number of
        // seconds later.
        public string[] Writes(string word, double seconds) =>
        [
            Programs.Python.Value!, "-c",
            "import sys, time\n" +
            "open(sys.argv[1], 'a').write(sys.argv[2] + ' start\\n')\n" +
            "time.sleep(float(sys.argv[3]))\n" +
            "open(sys.argv[1], 'a').write(sys.argv[2] + ' end\\n')",
            Log, word, seconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ];

        public string[] Lines() => File.Exists(Log) ? File.ReadAllLines(Log) : [];

        // The gate run in the background, its output read as it comes.
        public (Process Process, StringBuilder Output) Begin(params string[] arguments)
        {
            var start = new ProcessStartInfo(Programs.Python.Value!)
            {
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
            };
            foreach (var argument in (string[])["-I", Programs.ScriptPath("gpu.py"), "--lock=" + Lock, "--waiting=" + Waiting, .. arguments])
                start.ArgumentList.Add(argument);
            var output = new StringBuilder();
            var process = Process.Start(start)!;
            process.OutputDataReceived += (_, e) => { lock (output) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { lock (output) output.AppendLine(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            started.Add(process);
            return (process, output);
        }

        public (int Code, string Output) Run(params string[] arguments) =>
            Programs.Attempt("gpu.py", [], ["--lock=" + Lock, "--waiting=" + Waiting, .. arguments]);

        public string[] Marks() => Directory.Exists(Waiting) ? Directory.GetFiles(Waiting).Select(path => Path.GetFileName(path)).ToArray() : [];

        public void Dispose()
        {
            foreach (var process in started)
            {
                if (!process.HasExited) process.Kill(true);
                process.Dispose();
            }
            Directory.Delete(Folder, true);
        }
    }

    private static void Until(Func<bool> condition, string what, int seconds = 20)
    {
        var end = DateTime.UtcNow.AddSeconds(seconds);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < end, $"{what} did not come within {seconds} s.");
            Thread.Sleep(100);
        }
    }

    [NeedsPythonAndFlockFact]
    public void A_Waiting_Exclusive_Caller_Goes_Before_A_Later_Shared_One()
    {
        using var gate = new Gate();
        var (holder, _) = gate.Begin(["shared", "--", .. gate.Writes("held", 3)]);
        Until(() => gate.Lines().Contains("held start"), "the shared holder's start");

        var (timing, timed) = gate.Begin(["exclusive", "--", .. gate.Writes("timing", 1)]);
        Until(() => gate.Marks().Length == 1, "the exclusive caller's mark");
        Assert.Equal($"{Path.GetFileName(Programs.Root())}-{timing.Id}", gate.Marks()[0]);

        // Asked while the timing waits behind the first holder, so a gate that took the lock
        // shared at once would start it before the timing.
        var (later, _) = gate.Begin(["shared", "--", .. gate.Writes("later", 0)]);
        Assert.True(later.WaitForExit(30000) && timing.WaitForExit(1000) && holder.WaitForExit(1000), "the three runs ended");

        Assert.Equal(["held start", "held end", "timing start", "timing end", "later start", "later end"], gate.Lines());
        Assert.Equal(0, timing.ExitCode);
        Assert.Empty(gate.Marks());
        Assert.Contains("took the lock exclusive", timed.ToString());
    }

    [NeedsPythonAndFlockFact]
    public void A_Mark_Whose_Process_Is_Gone_Is_Cleared_And_The_Run_Goes_On()
    {
        using var gate = new Gate();
        using var gone = Process.Start(new ProcessStartInfo(Programs.Python.Value!, "-c pass"))!;
        gone.WaitForExit();
        Directory.CreateDirectory(gate.Waiting);
        File.WriteAllText(Path.Combine(gate.Waiting, $"Sample-{gone.Id}"), "");

        var (code, output) = gate.Run(["shared", "--", .. gate.Writes("run", 0)]);

        Assert.True(code == 0, output);
        Assert.Empty(gate.Marks());
        Assert.Contains($"removed the mark Sample-{gone.Id}, its process gone", output);
        Assert.Equal(["run start", "run end"], gate.Lines());
    }

    [NeedsPythonAndFlockFact]
    public void A_Living_Mark_Holds_Shared_Runs_Back_Past_Fifteen_Minutes()
    {
        using var gate = new Gate();
        Directory.CreateDirectory(gate.Waiting);
        // A mark of the test's own process, which lives, put twenty minutes back.
        var mark = Path.Combine(gate.Waiting, $"Sample-{Environment.ProcessId}");
        File.WriteAllText(mark, "");
        File.SetLastWriteTimeUtc(mark, DateTime.UtcNow.AddMinutes(-20));

        var (status, said) = gate.Run("status");
        Assert.True(status == 0, said);
        Assert.Contains($"waiting Sample-{Environment.ProcessId}, Sample's pid {Environment.ProcessId}, 20 min old, its process living", Programs.Lines(said));

        var (run, output) = gate.Begin(["shared", "--", .. gate.Writes("run", 0)]);
        Thread.Sleep(3000);
        Assert.Empty(gate.Lines());
        Assert.True(File.Exists(mark), "the mark stands");
        Assert.Contains("waits for 1 mark", output.ToString());

        File.Delete(mark);
        Assert.True(run.WaitForExit(20000), "the run started once the mark came down");
        Assert.Equal(["run start", "run end"], gate.Lines());
    }

    [NeedsPythonAndFlockFact]
    public void A_Run_Past_Its_Bound_Is_Stopped_And_Its_Lock_And_Mark_Let_Go()
    {
        using var gate = new Gate();
        // A bound of three seconds, given as a part of a minute.
        var (code, output) = gate.Run(["exclusive", "--minutes", "0.05", "--", .. gate.Writes("long", 60)]);

        Assert.Equal(124, code);
        Assert.Contains("stopped past its bound of 0.05 min", output);
        Assert.Equal(["long start"], gate.Lines());
        Assert.Empty(gate.Marks());
        var (next, after) = gate.Run(["exclusive", "--", .. gate.Writes("next", 0)]);
        Assert.True(next == 0, after);
    }

    [NeedsPythonAndFlockFact]
    public void Shared_Parts_Let_The_Lock_Go_Between_Them()
    {
        using var gate = new Gate();
        var (parts, _) = gate.Begin(["shared", "--", .. gate.Writes("first", 2), "--", .. gate.Writes("second", 0)]);
        Until(() => gate.Lines().Contains("first start"), "the first part's start");
        var (timing, _) = gate.Begin(["exclusive", "--", .. gate.Writes("timing", 0)]);
        Until(() => gate.Marks().Length == 1, "the exclusive caller's mark");

        Assert.True(parts.WaitForExit(30000) && timing.WaitForExit(1000), "the runs ended");
        Assert.Equal(["first start", "first end", "timing start", "timing end", "second start", "second end"], gate.Lines());
        Assert.Equal(0, parts.ExitCode);
    }

    [NeedsPythonAndFlockFact]
    public void Status_Names_The_Holders_And_The_Waiters()
    {
        using var gate = new Gate();
        var (holder, _) = gate.Begin(["shared", "--", .. gate.Writes("held", 30)]);
        Until(() => gate.Lines().Contains("held start"), "the holder's start");
        var (timing, _) = gate.Begin(["exclusive", "--", .. gate.Writes("timing", 0)]);
        Until(() => gate.Marks().Length == 1, "the exclusive caller's mark");

        var (code, said) = gate.Run("status");

        Assert.True(code == 0, said);
        var lines = Programs.Lines(said);
        Assert.Contains($"waiting {Path.GetFileName(Programs.Root())}-{timing.Id}, {Path.GetFileName(Programs.Root())}'s pid {timing.Id}, 0 min old, its process living", lines);
        // Linux lists a lock's holders in /proc/locks; macOS gives no such list.
        if (OperatingSystem.IsLinux())
            Assert.Contains(lines, line => line.StartsWith($"holding shared pid {holder.Id}, ", StringComparison.Ordinal) && line.EndsWith(" min old", StringComparison.Ordinal));
        else
            Assert.Contains("holders: not read, this system lists no locks", lines);
    }
}
