using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;

namespace Tools.Tests;

/// <summary>
/// Skipped where the checkout holds part of its history alone, so a commit the README names may
/// lie past what it holds.
/// </summary>
public sealed class NeedsWholeHistoryFactAttribute : FactAttribute
{
    public NeedsWholeHistoryFactAttribute()
    {
        var shallow = Programs.Run("git", Programs.Root(), "rev-parse", "--is-shallow-repository").Trim();
        if (shallow != "false") Skip = "The checkout holds part of its history alone, so the commits the README names are not read.";
    }
}

/// <summary>
/// "What a module carries" is the list a module pins against, so its table names every fact the
/// README's blocks give a module and no other, each with the check its block calls, and every
/// commit it names is in the history.
/// </summary>
public partial class CarriesTests
{
    private static readonly string Readme = File.ReadAllText(Path.Combine(Programs.Root(), "README.md")).ReplaceLineEndings("\n");

    [Fact]
    public void The_Table_Names_Every_Fact_The_Blocks_Give_With_The_Check_It_Calls_And_No_Other()
    {
        Assert.Empty(Faults(Readme));
    }

    [NeedsWholeHistoryFact]
    public void Every_Commit_The_Table_Names_Is_In_The_History()
    {
        var unknown = Commits(Readme).Where(hash => !IsAncestor(hash)).ToList();

        Assert.True(unknown.Count == 0, $"The table names {string.Join(", ", unknown)}, which the history of the head holds not.");
    }

    [Fact]
    public void A_Fact_Without_Its_Row_A_Row_Without_Its_Fact_And_A_Row_Naming_Another_Check_Are_Found()
    {
        var readme = """
            ## Binding it in your engine

            ```csharp
            [Fact]
            public void The_First()
            {
                var fault = ModuleChecks.FirstFault(Assembly.Load(Library));
                Assert.True(fault is null, fault);
            }

            [Fact]
            public void The_Second()
            {
                var fault = ModuleChecks.SecondFault(Assembly.Load(Library));
                Assert.True(fault is null, fault);
            }
            ```

            ### What a module carries

            | fact | the check it calls | in its form since | what changed it after |
            |---|---|---|---|
            | `The_First` | `ModuleChecks.OtherFault` | `abc1234` | |
            | `The_Third` | `ModuleChecks.ThirdFault` | `abc1234` | |

            ## Building
            """;

        Assert.Equal(
            [
                "The_First calls ModuleChecks.FirstFault where the table names ModuleChecks.OtherFault.",
                "The_Second has no row in the table.",
                "The_Third has a row in the table and no block gives it.",
            ],
            Faults(readme));
    }

    // What the table and the blocks of the README's section Binding it in your engine disagree on.
    private static List<string> Faults(string readme)
    {
        var start = readme.IndexOf("## Binding it in your engine", StringComparison.Ordinal);
        var end = readme.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        var section = readme[start..(end < 0 ? readme.Length : end)];
        var given = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match block in Block().Matches(section))
            foreach (Match fact in Fact().Matches(block.Groups[1].Value))
                given[fact.Groups["name"].Value] = fact.Groups["check"].Value;

        var table = section[section.IndexOf("### What a module carries", StringComparison.Ordinal)..];
        var rows = Row().Matches(table).ToDictionary(row => row.Groups["name"].Value, row => row.Groups["check"].Value, StringComparer.Ordinal);

        var faults = new List<string>();
        foreach (var (name, check) in given)
            if (!rows.TryGetValue(name, out var named)) faults.Add($"{name} has no row in the table.");
            else if (named != check) faults.Add($"{name} calls {check} where the table names {named}.");
        foreach (var name in rows.Keys.Where(name => !given.ContainsKey(name)))
            faults.Add($"{name} has a row in the table and no block gives it.");
        return faults;
    }

    private static IEnumerable<string> Commits(string readme)
    {
        var table = readme[readme.IndexOf("### What a module carries", StringComparison.Ordinal)..];
        table = table[..table.IndexOf("\n## ", StringComparison.Ordinal)];
        return Hash().Matches(table).Select(match => match.Groups[1].Value).Distinct();
    }

    private static bool IsAncestor(string hash)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = Programs.Root(), RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "merge-base", "--is-ancestor", hash, "HEAD" }) start.ArgumentList.Add(argument);
        using var git = Process.Start(start)!;
        git.WaitForExit();
        return git.ExitCode == 0;
    }

    [GeneratedRegex(@"```csharp\n(.*?)```", RegexOptions.Singleline)]
    private static partial Regex Block();

    [GeneratedRegex(@"\[Fact\]\s*public void (?<name>\w+)\(\)\s*\{[^}]*?(?<check>ModuleChecks\.\w+)", RegexOptions.Singleline)]
    private static partial Regex Fact();

    [GeneratedRegex(@"^\| `(?<name>\w+)` \| `(?<check>ModuleChecks\.\w+)` \|", RegexOptions.Multiline)]
    private static partial Regex Row();

    [GeneratedRegex(@"`([0-9a-f]{7,40})`")]
    private static partial Regex Hash();
}
