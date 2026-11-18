using Xunit;

namespace Tools.Tests;

public class CommentTests
{
    private const int Columns = 100;

    // The folders the build, the tests and the tools write into, and the ignored ones, hold no
    // source of the module's.
    private static readonly HashSet<string> LeftOut = new(StringComparer.Ordinal) { ".git", "bin", "obj", "TestResults", ".ref", ".claude", ".vs", ".idea", "dump" };

    [Fact]
    public void Every_Comment_Line_Of_The_Sources_Holds_To_100_Columns()
    {
        var root = Programs.Root();
        var found = new List<string>();
        foreach (var file in Sources(root))
        {
            var lines = File.ReadAllLines(file);
            var inXml = false;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var comment = IsComment(Path.GetExtension(file), line, ref inXml);
                if (comment is not null && line.Length > Columns && !IsAddress(comment))
                    found.Add($"{Path.GetRelativePath(root, file)}:{i + 1} runs to {line.Length} columns");
            }
        }

        Assert.True(found.Count == 0, "Comment lines past 100 columns (NORM.md, N 4.11):\n  " + string.Join("\n  ", found));
    }

    [Theory]
    [InlineData(".cs", "    /// <summary>A summary.</summary>", "<summary>A summary.</summary>")]
    [InlineData(".cs", "        // A line comment.", "A line comment.")]
    [InlineData(".cs", "        var x = 1; // after code", null)]
    [InlineData(".py", "    # A Python comment.", "A Python comment.")]
    [InlineData(".py", "print('# not a comment')", null)]
    [InlineData(".yml", "  # A workflow's comment.", "A workflow's comment.")]
    [InlineData(".csproj", "    <!-- One line of XML. -->", "One line of XML.")]
    [InlineData(".csproj", "    <TargetFramework>net10.0</TargetFramework>", null)]
    public void A_Comment_Line_Is_Told_From_Code_By_Its_Mark(string extension, string line, string? comment)
    {
        var inXml = false;
        Assert.Equal(comment, IsComment(extension, line, ref inXml));
    }

    [Fact]
    public void Every_Line_Of_An_Xml_Comment_Is_A_Comment_Line()
    {
        var inXml = false;
        Assert.Equal("First line,", IsComment(".props", "    <!-- First line,", ref inXml));
        Assert.Equal("the second line.", IsComment(".props", "         the second line. -->", ref inXml));
        Assert.Null(IsComment(".props", "    <Nullable>enable</Nullable>", ref inXml));
    }

    [Fact]
    public void A_Line_Holding_An_Address_Alone_Is_Free_Of_The_Width()
    {
        Assert.True(IsAddress("https://example.com/" + new string('a', 120)));
        Assert.False(IsAddress("See https://example.com/ for the rest."));
    }

    private static IEnumerable<string> Sources(string folder)
    {
        foreach (var file in Directory.EnumerateFiles(folder))
            if (Marks.ContainsKey(Path.GetExtension(file)) || Path.GetFileName(file) is ".gitignore" or ".gitattributes")
                yield return file;
        foreach (var child in Directory.EnumerateDirectories(folder))
            if (!LeftOut.Contains(Path.GetFileName(child)))
                foreach (var file in Sources(child))
                    yield return file;
    }

    // The mark a line comment opens with, by the file's extension, an empty one standing for XML,
    // whose comments open with <!-- and may run over several lines.
    private static readonly Dictionary<string, string> Marks = new(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"] = "//", [".py"] = "#", [".sh"] = "#", [".yml"] = "#", [".yaml"] = "#",
        [".csproj"] = "", [".props"] = "", [".targets"] = "", [".slnx"] = "", [".xml"] = "",
    };

    /// <summary>The text of a comment line, or null where the line is code.</summary>
    private static string? IsComment(string extension, string line, ref bool inXml)
    {
        var text = line.Trim();
        if (Marks.GetValueOrDefault(extension, "#") is { Length: > 0 } mark)
            return text.StartsWith(mark, StringComparison.Ordinal) ? text[mark.Length..].TrimStart('/').Trim() : null;

        if (!inXml && !text.StartsWith("<!--", StringComparison.Ordinal))
            return null;
        var opened = text.StartsWith("<!--", StringComparison.Ordinal) ? text[4..] : text;
        inXml = !opened.Contains("-->", StringComparison.Ordinal);
        return opened.Replace("-->", "", StringComparison.Ordinal).Trim();
    }

    private static bool IsAddress(string comment) =>
        !comment.Contains(' ', StringComparison.Ordinal) && (comment.StartsWith("https://", StringComparison.Ordinal) || comment.StartsWith("http://", StringComparison.Ordinal));
}
