using Xunit;

namespace Tools.Tests;

public class DocumentCopiesTests
{
    private const string Source = """
        # Style

        Every public type and member of the module has a summary, because an editor shows it when
        the member is hovered.

        A comment that restates the code is removed.

        ```bash
        grep -rn "x" .

        grep -rn "y" .
        ```
        """;

    [Fact]
    public void The_Sources_Carried_Are_The_Documents_Of_The_Checkout()
    {
        foreach (var name in DocumentCopies.Names)
            Assert.Equal(File.ReadAllText(Path.Combine(Programs.Root(), ".github", name)), DocumentCopies.Source(name));
        Assert.Contains("The middle dot `\u00b7` is not used either.", DocumentCopies.Source("STYLE.md"));
    }

    [Fact]
    public void A_Copy_That_Adds_A_Paragraph_And_Wraps_Its_Lines_Again_Holds_The_Source()
    {
        var copy = """
            # Style

            Every public type and member of the module has a summary, because an editor shows
            it when the member is hovered.

            What a generator writes is internal.

            A comment that restates the code is removed.

            ```bash
            grep -rn "x" .

            grep -rn "y" .
            ```

            A paragraph of the copy's own at its end.
            """;

        Assert.Empty(DocumentCopies.Differences(Source, copy));
    }

    [Fact]
    public void A_Changed_Word_And_A_Dropped_Paragraph_Are_Named_By_Their_First_Words()
    {
        var copy = """
            # Style

            Every public type and member of the module has a summary, because an IDE shows it when
            the member is hovered.

            ```bash
            grep -rn "x" .

            grep -rn "y" .
            ```
            """;

        Assert.Equal(
            [
                "The copy lacks or changes the paragraph that begins \"Every public type and member of the module ...\".",
                "The copy lacks or changes the paragraph that begins \"A comment that restates the code is removed.\".",
            ],
            DocumentCopies.Differences(Source, copy));
    }

    [Fact]
    public void A_Fenced_Block_Is_One_Paragraph_With_Its_Lines_And_A_Changed_Line_Is_Named()
    {
        var copy = Source.Replace("grep -rn \"y\" .", "grep -rn \"z\" .", StringComparison.Ordinal);

        Assert.Equal("```bash\ngrep -rn \"x\" .\n\ngrep -rn \"y\" .\n```", DocumentCopies.Paragraphs(Source)[^1]);
        Assert.Equal(["The copy lacks or changes the paragraph that begins \"```bash grep -rn \"x\" . grep -rn \"y\" ...\"."], DocumentCopies.Differences(Source, copy));
    }

    [Fact]
    public void A_Paragraph_Out_Of_The_Sources_Order_Is_Named()
    {
        var paragraphs = DocumentCopies.Paragraphs(Source);
        var copy = string.Join("\n\n", paragraphs[0], paragraphs[2], paragraphs[1], paragraphs[3]);

        Assert.Equal(["The copy holds out of the source's order the paragraph that begins \"A comment that restates the code is removed.\"."], DocumentCopies.Differences(Source, copy));
    }

    [Fact]
    public void The_Copys_Words_Are_Put_In_For_The_Modules()
    {
        var copy = Source.Replace("of the module has", "of the engine has", StringComparison.Ordinal);

        Assert.Single(DocumentCopies.Differences(Source, copy));
        Assert.Empty(DocumentCopies.Differences(Source, copy, [new("module", "engine")]));
    }

    [Fact]
    public void A_Document_The_Module_Carries_No_Source_Of_Is_Refused_By_Name()
    {
        var refused = Assert.Throws<ArgumentException>(() => DocumentCopies.Source("NORM.md"));
        Assert.Contains("NORM.md", refused.Message);
    }
}
