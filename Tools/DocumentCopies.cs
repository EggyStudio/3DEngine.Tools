using System.Text.RegularExpressions;

namespace Tools;

/// <summary>
/// The sources of the documents every repository carries a copy of, STYLE.md and COMMITS.md, and
/// the check of a copy against its source.
/// </summary>
/// <remarks>
/// <para>
/// A copy holds every paragraph of its source, in the source's order, with the repository's words
/// put in for the module's, and may add paragraphs of its own between them. A paragraph is the
/// text between blank lines, read with its lines joined, so a copy whose words are longer wraps its
/// lines again and still holds it. A fenced block is one paragraph with its lines kept, blank ones
/// too.
/// </para>
/// <para>
/// A copy that changes a paragraph of the source for its own reasons sets its own text apart as a
/// paragraph of its own, or the change is made in the source, so a rule mended in one repository is
/// not left behind in the others.
/// </para>
/// </remarks>
public static partial class DocumentCopies
{
    /// <summary>The names of the documents the module carries the sources of.</summary>
    public static IReadOnlyList<string> Names { get; } = ["STYLE.md", "COMMITS.md"];

    /// <summary>The source of a document by its name, as <see cref="Names"/> gives it.</summary>
    /// <exception cref="ArgumentException">The module carries no source of that name.</exception>
    public static string Source(string name)
    {
        using var stream = typeof(DocumentCopies).Assembly.GetManifestResourceStream(name)
            ?? throw new ArgumentException($"The module carries no source named {name}, only {string.Join(" and ", Names)}.", nameof(name));
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Each paragraph of a source a copy lacks or holds changed or out of order, named by its first
    /// words, or none where the copy holds them all.
    /// </summary>
    /// <param name="source">The source's text.</param>
    /// <param name="copy">The copy's text.</param>
    /// <param name="words">
    /// The words the copy puts in place of the source's, as <c>module</c> and <c>engine</c>, each
    /// replaced as a whole word in the given order, or none.
    /// </param>
    public static IReadOnlyList<string> Differences(string source, string copy, IEnumerable<KeyValuePair<string, string>>? words = null)
    {
        var replacing = (words ?? []).ToList();
        var copied = Paragraphs(copy);
        var differences = new List<string>();
        int next = 0;
        foreach (var paragraph in Paragraphs(source))
        {
            var expected = replacing.Aggregate(paragraph, (text, pair) =>
                Regex.Replace(text, $@"\b{Regex.Escape(pair.Key)}\b", pair.Value.Replace("$", "$$", StringComparison.Ordinal)));
            var at = copied.IndexOf(expected, next);
            if (at >= 0)
                next = at + 1;
            else if (copied.IndexOf(expected) >= 0)
                differences.Add($"The copy holds out of the source's order the paragraph that begins \"{Opening(expected)}\".");
            else
                differences.Add($"The copy lacks or changes the paragraph that begins \"{Opening(expected)}\".");
        }

        return differences;
    }

    /// <summary>
    /// A text's paragraphs, each with its lines joined, a fenced block's kept as they are.
    /// </summary>
    /// <remarks>Public for the tests of a copy that would read where a paragraph breaks.</remarks>
    public static List<string> Paragraphs(string text)
    {
        var paragraphs = new List<string>();
        var lines = new List<string>();
        bool fenced = false;
        foreach (var raw in text.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                if (!fenced) Close(paragraphs, lines, false);
                lines.Add(line);
                if (fenced) Close(paragraphs, lines, true);
                fenced = !fenced;
            }
            else if (fenced)
                lines.Add(line);
            else if (line.Length == 0)
                Close(paragraphs, lines, false);
            else
                lines.Add(line);
        }

        Close(paragraphs, lines, fenced);
        return paragraphs;
    }

    private static void Close(List<string> paragraphs, List<string> lines, bool keepLines)
    {
        if (lines.Count == 0) return;
        paragraphs.Add(keepLines ? string.Join('\n', lines) : Space().Replace(string.Join(' ', lines.Select(line => line.Trim())), " "));
        lines.Clear();
    }

    private static string Opening(string paragraph)
    {
        var opening = string.Join(' ', paragraph.Split([' ', '\n'], StringSplitOptions.RemoveEmptyEntries).Take(8));
        return opening.Length < paragraph.Length ? opening + " ..." : opening;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Space();
}
