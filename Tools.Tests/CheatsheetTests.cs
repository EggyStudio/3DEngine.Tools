using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace Tools.Tests;

public class CheatsheetTests
{
    private static readonly string Root = Programs.Root();
    private static readonly Assembly Library = typeof(Cheatsheet).Assembly;
    private static readonly string Readme = File.ReadAllText(Path.Combine(Root, "README.md"));

    // The README's block from here to the end marker, word for word.
    [Fact]
    public void The_Cheatsheet_Is_What_The_Built_Library_Writes()
    {
        var fault = ModuleChecks.CheatsheetFault(Assembly.Load(Library.GetName().Name!), Readme, Path.Combine(Root, "CHEATSHEET.md"));
        Assert.True(fault is null, fault);
    }
    // The end marker.

    [Fact]
    public void Every_Line_Of_This_Librarys_Page_Holds_Its_Summarys_First_Sentence()
    {
        var page = Cheatsheet.Write(Library, Readme, "");
        var lines = Block(page).Where(line => line.Length > 0).ToList();

        // A record's constructor alone goes without, its type's line saying what it would.
        Assert.All(lines, line => Assert.True(line.Contains("// ", StringComparison.Ordinal) || System.Text.RegularExpressions.Regex.IsMatch(line, @"^[A-Z]\w*\("), $"{line} has no summary"));
        Assert.All(lines, line => Assert.DoesNotContain("public ", line.Split("//")[0]));
    }

    [Fact]
    public void A_Declaration_Is_Written_As_The_Source_Writes_It_Without_Its_Modifiers()
    {
        var lines = Block(Cheatsheet.Write(Library, Readme, "")).Select(line => line.Split("//")[0].TrimEnd()).ToList();

        Assert.Contains("class ShellForms", lines);
        Assert.Contains("record ShellForm", lines);
        Assert.Contains("ShellForm(string Name, Regex Pattern, string Instead);", lines);
        Assert.Contains("string Name { get; init; }", lines);
        Assert.Contains("IReadOnlyList<ShellFinding> Find(string root, IEnumerable<string> scripts);", lines);
        Assert.Contains("SortedSet<string> Scripts(string root, params string[] leftOut);", lines);
        Assert.Contains("string? ReferenceFault(IEnumerable<string> references, IEnumerable<string>? allowed = null);", lines);
        Assert.Contains($"string Marker = \"{Cheatsheet.Marker}\";", lines);
        Assert.Contains("string ToString();", lines);
    }

    [Fact]
    public void A_First_Page_Is_Titled_With_Cheatsheet_As_One_Word()
    {
        Assert.StartsWith("# 3DEngine.Tools cheatsheet\n", Cheatsheet.Write(Library, Readme, ""));
    }

    [Fact]
    public void A_Records_Constructor_Leaves_Out_The_Summary_Its_Type_Gives()
    {
        var lines = Block(Cheatsheet.Write(Library, Readme, ""));

        Assert.Contains("ShellForm(string Name, Regex Pattern, string Instead);", lines);
        Assert.Contains("ShellFinding(string Script, int Line, ShellForm Form);", lines);
        Assert.Contains(lines, line => line.StartsWith("record ShellForm ", StringComparison.Ordinal) && line.Contains("// A form of shell", StringComparison.Ordinal));
    }

    [Fact]
    public void A_Member_That_Inherits_Its_Documentation_Takes_The_Interfaces_Or_The_Bases_Summary()
    {
        var lines = SampleLines();

        Assert.Contains("double Area { get; }  // The shape's area in square meters", lines);
        Assert.Contains("IShape Scaled(double by);  // The shape made larger by a factor", lines);
        Assert.Contains("double Size { get; }  // The shape's area in square meters", lines);
        Assert.Contains("string Name();  // The thing's name as a reader reads it", lines);
    }

    [Fact]
    public void A_Positional_Record_Is_Its_Types_Summary_And_Its_Parameters_With_A_Bare_Constructor()
    {
        var lines = SampleLines();

        Assert.Contains("record struct Planted  // A place on the ground and its seed", lines);
        Assert.Contains("Planted(Vector3 Place, uint Seed);", lines);
        Assert.Contains("Vector3 Place { get; init; }  // Where it stands, in meters", lines);
        Assert.Contains("uint Seed { get; init; }  // The seed it came with", lines);
    }

    [Fact]
    public void An_Infinite_Or_Not_A_Number_Default_Is_Written_As_CSharp_Names_It()
    {
        Assert.Contains("float Reach(float far = float.PositiveInfinity, double near = double.NegativeInfinity, float none = float.NaN);  // The first thing the ray meets within its reach",
            SampleLines());
    }

    [Fact]
    public void A_Generic_Type_Is_Written_With_Its_Parameters_And_Named_By_Its_Plain_Name()
    {
        var lines = SampleLines();

        Assert.Contains("class Pair<TKey, TValue>  // A pair of a key and a value", lines);
        Assert.Contains("TKey? Key { get; init; }  // The pair's key", lines);
    }

    // The samples' lines as the writer writes them from the tests' own documentation file, the
    // tests' other types among them, each comment two blanks after its declaration.
    private static List<string> SampleLines() =>
        [.. Block(Cheatsheet.Write(typeof(Samples.Square).Assembly, "`IShape` `Square` `Named` `Word` `Planted` `Ray` `Pair<TKey, TValue>`", ""))
            .Select(line => System.Text.RegularExpressions.Regex.Replace(line, @"\s{2,}// ", "  // "))];

    [Fact]
    public void The_Types_Stand_In_The_Order_The_Readme_First_Names_Them()
    {
        var readme = "# Sample\n\nFirst `DocumentCopies`, then `ShellForms` and `ShellForm.Name`.\n";
        var types = Block(Cheatsheet.Write(Library, readme, "")).Where(line => line.StartsWith("class ", StringComparison.Ordinal) || line.StartsWith("record ", StringComparison.Ordinal))
            .Select(line => line.Split("//")[0].Trim()).ToList();

        Assert.Equal(["class DocumentCopies", "class ShellForms", "record ShellForm"], types[..3]);
        Assert.Equal(types.Count, types.Distinct().Count());
    }

    [Fact]
    public void The_Text_Outside_The_List_Is_Kept_And_The_List_Written_Again()
    {
        var page = $"# Hand\n\nA paragraph by hand.\n\n{Cheatsheet.Marker}\n```csharp\nvoid Stale();\n```\n\nA closing line by hand.\n";

        var written = Cheatsheet.Write(Library, Readme, page);

        Assert.StartsWith($"# Hand\n\nA paragraph by hand.\n\n{Cheatsheet.Marker}\n```csharp\n", written);
        Assert.EndsWith("```\n\nA closing line by hand.\n", written);
        Assert.DoesNotContain("Stale", written);
        Assert.Equal(written, Cheatsheet.Write(Library, Readme, written));
    }

    [Fact]
    public void A_Page_That_Differs_Or_Lost_Its_Marker_Is_A_Fault_Naming_What_To_Do()
    {
        var folder = Directory.CreateTempSubdirectory("cheatsheet-").FullName;
        try
        {
            var page = Path.Combine(folder, "CHEATSHEET.md");
            File.WriteAllText(page, Cheatsheet.Write(Library, Readme, "").Replace("// The shell forms", "// Shell forms", StringComparison.Ordinal));
            var stale = ModuleChecks.CheatsheetFault(Library, Readme, page);
            File.WriteAllText(page, "# A page by hand alone\n");
            var unmarked = ModuleChecks.CheatsheetFault(Library, Readme, page);
            File.WriteAllText(page, Cheatsheet.Write(Library, Readme, ""));

            Assert.NotNull(stale);
            Assert.Matches(@"^CHEATSHEET\.md differs at line \d+ from what the built library writes, "".*// Shell forms", stale);
            Assert.EndsWith("Run the tests with CHEATSHEET_WRITE=1 to write it again.", stale);
            Assert.StartsWith("CHEATSHEET.md cannot be written again: The page holds no line", unmarked);
            Assert.Null(ModuleChecks.CheatsheetFault(Library, Readme, page));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void A_Members_Summary_Is_Its_First_Sentence_With_Its_References_As_Names()
    {
        var documentation = XDocument.Parse($"""
            <doc><members>
                <member name="T:Tools.ShellForms"><summary>Reads <see cref="T:Tools.ShellForm"/>s for <paramref name="root"/>. A second sentence.</summary></member>
            </members></doc>
            """);

        var first = Block(Cheatsheet.Write(Library, documentation, Readme, "")).Single(line => line.StartsWith("class ShellForms", StringComparison.Ordinal));

        Assert.EndsWith("// Reads ShellForms for root", first);
    }

    private static string[] Block(string page)
    {
        var lines = page.ReplaceLineEndings("\n").Split('\n');
        var open = Array.IndexOf(lines, Cheatsheet.Marker) + 2;
        return lines[open..Array.IndexOf(lines, "```", open)];
    }
}
