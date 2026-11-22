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
            var (code, printed) = Programs.Attempt("usage.py", east, "2026-11-18", "--projects=" + projects, "--cache=" + Path.Combine(projects, ".usage-cache"));

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

            var (since, from17) = Programs.Attempt("usage.py", east, "2026-11-18", "--since=17", "--projects=" + projects, "--cache=" + Path.Combine(projects, ".usage-cache"));
            Assert.True(since == 0, from17);
            Assert.Contains("all  turns 3  cache read 5,500  cache written 0  output 207", Programs.Lines(from17));
            Assert.DoesNotContain("16h", from17);

            var (missing, said) = Programs.Attempt("usage.py", [], "--projects=" + Path.Combine(projects, "none"), "--cache=" + Path.Combine(projects, ".usage-cache"));
            Assert.Equal(2, missing);
            Assert.Contains("does not stand", said);
        }
        finally
        {
            Directory.Delete(projects, true);
        }
    }

    [NeedsPythonFact]
    public void A_Growing_Transcript_Is_Read_Only_From_Where_The_Last_Run_Ended_And_Gives_A_Cold_Runs_Totals()
    {
        var projects = Directory.CreateTempSubdirectory("usage-").FullName;
        try
        {
            var folder = Path.Combine(projects, "-a-project");
            Directory.CreateDirectory(folder);
            var transcript = Path.Combine(folder, "s1.jsonl");
            Dictionary<string, string?> utc = new() { ["TZ"] = "UTC0" };
            string[] Run(string cache, params string[] more)
            {
                var (code, printed) = Programs.Attempt("usage.py", utc, ["2026-11-18", "--projects=" + projects, "--cache=" + cache, .. more]);
                Assert.True(code == 0, printed);
                return Programs.Lines(printed);
            }
            string Cache(string[] lines) => lines.Single(l => l.StartsWith("cache: ", StringComparison.Ordinal));
            string[] Totals(string[] lines) => lines.Where(l => !l.StartsWith("cache: ", StringComparison.Ordinal)).ToArray();

            File.WriteAllText(transcript, Turn("s1", "r1", "2026-11-18T10:00:00Z", read: 1000) + "\n" + Turn("s1", "r2", "2026-11-18T11:00:00Z", read: 2000) + "\n");
            var first = new FileInfo(transcript).Length;
            var warm = Path.Combine(projects, ".warm");
            Assert.Equal($"cache: {first:N0} bytes read of {first:N0} in 1 transcripts", Cache(Run(warm)));

            // Two more requests and half of a third, which is still being written.
            var added = Turn("s1", "r3", "2026-11-18T12:00:00Z", read: 4000) + "\n" + Turn("s1", "r4", "2026-11-18T13:00:00Z", read: 8000) + "\n";
            var half = Turn("s1", "r5", "2026-11-18T14:00:00Z", read: 16000);
            File.AppendAllText(transcript, added + half[..40]);
            var second = new FileInfo(transcript).Length;
            var again = Run(warm);
            Assert.Equal($"cache: {added.Length:N0} bytes read of {second:N0} in 1 transcripts", Cache(again));
            Assert.Contains("all  turns 4  cache read 15,000  cache written 0  output 400", again);
            Assert.Equal(Totals(Run(Path.Combine(projects, ".cold1"))), Totals(again));

            // The rest of the third line, and then nothing appended, which reads nothing at all.
            File.AppendAllText(transcript, half[40..] + "\n");
            var third = Run(warm);
            Assert.StartsWith($"cache: {half.Length + 1:N0} bytes read of ", Cache(third));
            Assert.Contains("all  turns 5  cache read 31,000  cache written 0  output 500", third);
            Assert.Equal(Totals(Run(Path.Combine(projects, ".cold2"))), Totals(third));
            Assert.StartsWith("cache: 0 bytes read of ", Cache(Run(warm)));
            Assert.StartsWith("cache: 0 bytes read of ", Cache(Run(warm, "--since=11")));
            Assert.Contains("all  turns 4  cache read 30,000  cache written 0  output 400", Run(warm, "--since=11"));

            // A transcript written over from its start is read again whole.
            File.WriteAllText(transcript, Turn("s1", "q1", "2026-11-18T09:00:00Z", read: 5) + "\n");
            var over = Run(warm);
            Assert.Contains("all  turns 1  cache read 5  cache written 0  output 100", over);
            Assert.StartsWith($"cache: {new FileInfo(transcript).Length:N0} bytes read of ", Cache(over));
        }
        finally
        {
            Directory.Delete(projects, true);
        }
    }

    [NeedsPythonFact]
    public void A_Run_Stopped_Midway_Leaves_What_It_Committed_And_The_Next_Reads_Only_The_Rest()
    {
        var projects = Directory.CreateTempSubdirectory("usage-").FullName;
        try
        {
            var folder = Path.Combine(projects, "-a-project");
            Directory.CreateDirectory(folder);
            var transcript = Path.Combine(folder, "s1.jsonl");
            var turns = Enumerable.Range(1, 12).Select(i => Turn("s1", $"r{i}", $"2026-11-18T{9 + i / 2:00}:{i % 2 * 30:00}:00Z", read: i * 100));
            File.WriteAllText(transcript, string.Join("\n", turns) + "\n");
            var size = new FileInfo(transcript).Length;
            Dictionary<string, string?> utc = new() { ["TZ"] = "UTC0" };
            var warm = Path.Combine(projects, ".warm");

            // Stopped after the first commit past 1,000 bytes, with the cache holding those bytes.
            utc["USAGE_STOP_AFTER"] = "1000";
            var (stopped, said) = Programs.Attempt("usage.py", utc, "2026-11-18", "--projects=" + projects, "--cache=" + warm, "--batch-bytes=600");
            Assert.True(stopped == 3, said);
            utc.Remove("USAGE_STOP_AFTER");

            var (code, printed) = Programs.Attempt("usage.py", utc, "2026-11-18", "--projects=" + projects, "--cache=" + warm);
            var (coldCode, cold) = Programs.Attempt("usage.py", utc, "2026-11-18", "--projects=" + projects, "--cache=" + Path.Combine(projects, ".cold"));
            Assert.True(code == 0 && coldCode == 0, printed + cold);
            var reading = Programs.Lines(printed).Single(l => l.StartsWith("cache: ", StringComparison.Ordinal));
            var read = long.Parse(reading.Split(' ')[1].Replace(",", ""), System.Globalization.CultureInfo.InvariantCulture);
            Assert.InRange(read, 1, size - 1000);
            Assert.DoesNotContain("reread", printed);
            Assert.Equal(Programs.Lines(cold).Where(l => !l.StartsWith("cache: ", StringComparison.Ordinal)),
                         Programs.Lines(printed).Where(l => !l.StartsWith("cache: ", StringComparison.Ordinal)));
            Assert.Contains("all  turns 12  cache read 7,800  cache written 0  output 1,200", printed);
        }
        finally
        {
            Directory.Delete(projects, true);
        }
    }

    [NeedsPythonFact]
    public void A_Transcript_Read_Again_From_Its_Start_Is_Named_With_Its_Size_And_Why()
    {
        var projects = Directory.CreateTempSubdirectory("usage-").FullName;
        try
        {
            var folder = Path.Combine(projects, "-a-project");
            Directory.CreateDirectory(folder);
            var transcript = Path.Combine(folder, "s1.jsonl");
            Dictionary<string, string?> utc = new() { ["TZ"] = "UTC0" };
            var cache = Path.Combine(projects, ".warm");
            string Run() => Programs.Attempt("usage.py", utc, "2026-11-18", "--projects=" + projects, "--cache=" + cache).Output;
            string Write(params string[] requests)
            {
                File.WriteAllText(transcript, string.Concat(requests.Select(r => Turn("s1", r, "2026-11-18T10:00:00Z", read: 10) + "\n")));
                return transcript;
            }

            Write("r1", "r2", "r3");
            var first = new FileInfo(transcript).Length;
            Assert.DoesNotContain("reread", Run());

            // Written over by a shorter transcript, as a compaction does.
            Write("r4");
            var shorter = new FileInfo(transcript).Length;
            Assert.Contains($"reread {transcript}, {shorter:N0} bytes, shrunk from {first:N0} bytes", Run());

            // Written over by a longer one that starts otherwise, as a resume does.
            Write("q5", "q6", "q7");
            var longer = new FileInfo(transcript).Length;
            Assert.Contains($"reread {transcript}, {longer:N0} bytes, first bytes differ", Run());
            Assert.DoesNotContain("reread", Run());

            // The same bytes under a modified time that went back, which these files only growing
            // is no reason to read them again for.
            File.SetLastWriteTimeUtc(transcript, DateTime.UtcNow.AddDays(-3));
            Assert.DoesNotContain("reread", Run());

            // A change in the bytes just before the kept offset, beyond the first bytes the head
            // checks, at the same size and under a modified time that moved.
            var requests = Enumerable.Range(1, 40).Select(i => $"p{i:00}").ToArray();
            Write(requests);
            Run();
            var text = File.ReadAllText(transcript);
            File.WriteAllText(transcript, text.Remove(text.LastIndexOf("p40", StringComparison.Ordinal), 3).Insert(text.LastIndexOf("p40", StringComparison.Ordinal), "q40"));
            File.SetLastWriteTimeUtc(transcript, DateTime.UtcNow.AddDays(-2));
            var edited = new FileInfo(transcript).Length;
            Assert.Contains($"reread {transcript}, {edited:N0} bytes, bytes before the kept offset differ", Run());
            Assert.DoesNotContain("reread", Run());
        }
        finally
        {
            Directory.Delete(projects, true);
        }
    }
}
