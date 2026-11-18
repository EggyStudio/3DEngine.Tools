using System.Text.Json;
using Xunit;

namespace Tools.Tests;

public class UsageTests
{
    // An assistant entry of a request, its usage as the transcripts keep it.
    private static string Turn(string session, string request, string at, int read, int written = 0, int input = 10, int output = 100, bool side = false) =>
        JsonSerializer.Serialize(new
        {
            type = "assistant", sessionId = session, requestId = request, timestamp = at, isSidechain = side,
            message = new { usage = new { input_tokens = input, cache_creation_input_tokens = written, cache_read_input_tokens = read, output_tokens = output } },
        });

    [NeedsPythonFact]
    public void A_Days_Use_Is_Summed_By_Session_And_By_Hour_A_Request_Counted_Once()
    {
        var projects = Directory.CreateTempSubdirectory("usage-").FullName;
        try
        {
            var folder = Path.Combine(projects, "-a-project");
            Directory.CreateDirectory(Path.Combine(folder, "s1", "subagents"));
            File.WriteAllLines(Path.Combine(folder, "s1.jsonl"),
            [
                JsonSerializer.Serialize(new { type = "agent-name", agentName = "tools", sessionId = "s1" }),
                // One response written as two entries repeating its usage, counted once.
                Turn("s1", "r1", "2026-11-18T15:10:00Z", read: 1000, written: 90),
                Turn("s1", "r1", "2026-11-18T15:10:01Z", read: 1000, written: 90),
                Turn("s1", "r2", "2026-11-18T16:20:00Z", read: 3000),
                // Another day's request in local time, an hour east of UTC, left out.
                Turn("s1", "r0", "2026-11-17T22:00:00Z", read: 99999),
                "{ a broken line",
            ]);
            // A subagent's request, counted under the session it ran for.
            File.WriteAllLines(Path.Combine(folder, "s1", "subagents", "agent-a.jsonl"), [Turn("s1", "r3", "2026-11-18T16:30:00Z", read: 2000, side: true)]);
            File.WriteAllLines(Path.Combine(folder, "s2.jsonl"), [Turn("s2", "q1", "2026-11-18T16:40:00Z", read: 500, output: 7)]);

            // An hour east of UTC, in the form both POSIX and Windows read.
            Dictionary<string, string?> east = new() { ["TZ"] = "XXX-1" };
            var (code, printed) = Programs.Attempt("usage.py", east, "2026-11-18", "--projects=" + projects);

            Assert.True(code == 0, printed);
            var lines = Programs.Lines(printed);
            // Context is the input, the cache written and the cache read of a request.
            Assert.Contains("tools  turns 3  cache read 6,000  cache written 90  output 300  context mean 2,040  largest 3,010", lines);
            Assert.Contains("s2  turns 1  cache read 500  cache written 0  output 7  context mean 510  largest 510", lines);
            Assert.Contains("by hour, local time", lines);
            Assert.Contains("  16h  1 turns  1,000 read", lines);
            Assert.Contains("  17h  3 turns  5,500 read", lines);
            Assert.Contains("all  turns 4  cache read 6,500  cache written 90  output 307", lines);
            Assert.Contains(lines, line => line.StartsWith("1 line not read in ", StringComparison.Ordinal) && line.EndsWith("s1.jsonl", StringComparison.Ordinal));
            Assert.True(Array.IndexOf(lines, lines.First(l => l.StartsWith("tools ", StringComparison.Ordinal))) < Array.IndexOf(lines, lines.First(l => l.StartsWith("s2 ", StringComparison.Ordinal))), "the larger session stands first");

            var (since, from17) = Programs.Attempt("usage.py", east, "2026-11-18", "--since=17", "--projects=" + projects);
            Assert.True(since == 0, from17);
            Assert.Contains("all  turns 3  cache read 5,500  cache written 0  output 207", Programs.Lines(from17));
            Assert.DoesNotContain("16h", from17);

            var (missing, said) = Programs.Attempt("usage.py", [], "--projects=" + Path.Combine(projects, "none"));
            Assert.Equal(2, missing);
            Assert.Contains("does not stand", said);
        }
        finally
        {
            Directory.Delete(projects, true);
        }
    }
}
