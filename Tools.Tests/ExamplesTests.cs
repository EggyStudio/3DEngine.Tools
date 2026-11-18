using System.Text.Json;
using Xunit;

namespace Tools.Tests;

/// <summary>
/// The examples' page shows each layout of its folder word for word beside the picture it draws,
/// and every layout draws, so the page a stranger reads first is true of the scripts as they are.
/// </summary>
public class ExamplesTests
{
    private static readonly string Folder = Path.Combine(Programs.Root(), "examples");

    [NeedsPillowFact]
    public void Every_Layout_Of_The_Examples_Draws_With_Its_Script()
    {
        var output = Directory.CreateTempSubdirectory("examples-").FullName;
        try
        {
            foreach (var layout in Directory.EnumerateFiles(Folder, "*.json"))
            {
                // A sheet of captures is laid out by its rows, a sheet of charts by its panels.
                using var read = JsonDocument.Parse(File.ReadAllText(layout));
                var script = read.RootElement.TryGetProperty("rows", out _) ? "sheet.py" : "chart.py";
                var printed = Programs.Script(script, layout, Path.Combine(output, Path.GetFileNameWithoutExtension(layout) + ".png"));
                Assert.Contains("panel", printed);
            }
        }
        finally
        {
            Directory.Delete(output, true);
        }
    }

    [Fact]
    public void The_Page_Shows_Each_Layout_Word_For_Word_Beside_Its_Picture()
    {
        var page = File.ReadAllText(Path.Combine(Folder, "README.md")).ReplaceLineEndings("\n");
        var layouts = Directory.EnumerateFiles(Folder, "*.json").Order(StringComparer.Ordinal).ToList();
        Assert.NotEmpty(layouts);

        foreach (var layout in layouts)
        {
            var name = Path.GetFileNameWithoutExtension(layout);
            var text = File.ReadAllText(layout).ReplaceLineEndings("\n").TrimEnd('\n');
            Assert.Contains($"]({name}.webp)", page);
            Assert.True(File.Exists(Path.Combine(Folder, name + ".webp")), $"{name}.webp is not drawn");
            Assert.Contains($"examples/{name}.json examples/{name}.webp", page);
            Assert.Contains($"```json\n{text}\n```", page);
        }
    }
}
