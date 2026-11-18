using System.Text;
using System.Text.Json;
using Xunit;

namespace Tools.Tests;

public class SheetTests
{
    private const int Width = 64, Height = 32;

    [NeedsPillowFact]
    public void A_Sheet_Places_Its_Panels_On_White_And_Reads_Its_Boxes()
    {
        var folder = Directory.CreateTempSubdirectory("sheet-").FullName;
        try
        {
            // A frame at 0.25 with a square at 0.5 on the left and one at 0.125 in the middle, its
            // reference at 0.25 throughout, and two pictures, a square of the second 10 levels
            // lighter.
            WritePfm(Path.Combine(folder, "frame.pfm"), (x, y) => x < 16 && y < 16 ? 0.5f : x >= 32 && x < 48 && y < 16 ? 0.125f : 0.25f);
            WritePfm(Path.Combine(folder, "reference.pfm"), (_, _) => 0.25f);
            WritePpm(Path.Combine(folder, "first.ppm"), (_, _) => 128);
            WritePpm(Path.Combine(folder, "second.ppm"), (x, y) => x < 16 && y < 16 ? (byte)138 : (byte)128);
            var layout = new
            {
                font = 12,
                columns = new[] { "frame", "frame less reference", "two pictures" },
                rows = new object[]
                {
                    new
                    {
                        title = "One row",
                        panels = new object[]
                        {
                            new { image = "frame.pfm", boxes = new[] { new[] { 0, 0, 16, 16 }, new[] { 8, 0, 24, 16 } } },
                            new { signed = new[] { "frame.pfm", "reference.pfm" }, share = 0.5 },
                            new { difference = new[] { "first.ppm", "second.ppm" }, times = 4 },
                        },
                    },
                },
            };
            File.WriteAllText(Path.Combine(folder, "layout.json"), JsonSerializer.Serialize(layout));
            var sheetPath = Path.Combine(folder, "sheet.ppm");

            var printed = Programs.Script("sheet.py", Path.Combine(folder, "layout.json"), sheetPath);

            // The first box lies on the bright square, the second half on it and half off.
            Assert.Contains("row 1 panel 1 box 1 at 0,0,16,16 reads 0.5", printed.Split('\n'));
            Assert.Contains("row 1 panel 1 box 2 at 8,0,24,16 reads 0.375", printed.Split('\n'));
            var sheet = ReadPpm(sheetPath);
            Assert.Equal((255, 255, 255), sheet.At(0, 0));
            var (capture, signed, difference) = (Place(printed, 1), Place(printed, 2), Place(printed, 3));

            // 0.25 of linear light is 137 in sRGB, read away from the boxes and their readings.
            Assert.Equal((137, 137, 137), sheet.At(capture.X + 56, capture.Y + 28));
            Assert.Equal((255, 0, 0), sheet.At(signed.X + 8, signed.Y + 8));
            Assert.Equal((0, 0, 255), sheet.At(signed.X + 40, signed.Y + 8));
            Assert.Equal((215, 215, 215), sheet.At(difference.X + 8, difference.Y + 8));
            Assert.Equal((255, 255, 255), sheet.At(difference.X + 40, difference.Y + 20));

            // The signed difference is the sheet's one dark panel, black where the frame meets its
            // reference, which three of its corners do.
            Assert.Equal((0, 0, 0), sheet.At(signed.X + 56, signed.Y + 28));
            Assert.Equal((0, 0, 0), sheet.At(signed.X + Width - 1, signed.Y));
            Assert.Equal((0, 0, 0), sheet.At(signed.X, signed.Y + Height - 1));
            Assert.Equal((0, 0, 0), sheet.At(signed.X + Width - 1, signed.Y + Height - 1));

            // Every panel stands on the white ground, left, right and below.
            foreach (var (x, y) in new[] { capture, signed, difference })
            {
                Assert.Equal((255, 255, 255), sheet.At(x - 1, y + Height / 2));
                Assert.Equal((255, 255, 255), sheet.At(x + Width, y + Height / 2));
                Assert.Equal((255, 255, 255), sheet.At(x + Width / 2, y + Height));
            }

            // The row's title stands first, on a line of its own above the panels' labels and in a
            // larger type than theirs.
            var (title, labels) = (Line(printed, "title"), Line(printed, "labels"));
            Assert.True(title.Y + title.Height <= labels.Y, $"the title at {title.Y} runs into the labels at {labels.Y}");
            Assert.True(labels.Y + labels.Height <= capture.Y, $"the labels at {labels.Y} run into the panels at {capture.Y}");
            Assert.True(title.Height > labels.Height, $"the title's line is {title.Height} high and the labels' {labels.Height}");
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    private static (int Y, int Height) Line(string printed, string kind)
    {
        var line = printed.Split('\n').Single(l => l.StartsWith($"row 1 {kind} at ", StringComparison.Ordinal));
        var words = line.Split(' ');
        return (int.Parse(words[4].Split(',')[1]), int.Parse(words[6]));
    }

    private static (int X, int Y) Place(string printed, int panel)
    {
        var line = printed.Split('\n').Single(l => l.StartsWith($"row 1 panel {panel} at ", StringComparison.Ordinal));
        var at = line.Split(' ')[5].Split(',');
        return (int.Parse(at[0]), int.Parse(at[1]));
    }

    // PFM's rows run from the bottom up, and a negative scale marks its floats little-endian.
    private static void WritePfm(string path, Func<int, int, float> light)
    {
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write(Encoding.ASCII.GetBytes($"PF\n{Width} {Height}\n-1.0\n"));
        for (int y = Height - 1; y >= 0; y--)
            for (int x = 0; x < Width; x++)
                for (int c = 0; c < 3; c++)
                    writer.Write(light(x, y));
    }

    private static void WritePpm(string path, Func<int, int, byte> gray)
    {
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write(Encoding.ASCII.GetBytes($"P6\n{Width} {Height}\n255\n"));
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                writer.Write(new[] { gray(x, y), gray(x, y), gray(x, y) });
    }

    private sealed record Picture(int Width, byte[] Pixels)
    {
        public (int, int, int) At(int x, int y)
        {
            var i = 3 * (y * Width + x);
            return (Pixels[i], Pixels[i + 1], Pixels[i + 2]);
        }
    }

    // Pillow writes a binary PPM's header as three lines, P6, the size and the largest value.
    private static Picture ReadPpm(string path)
    {
        var bytes = File.ReadAllBytes(path);
        int at = 0;
        string Token()
        {
            while (char.IsWhiteSpace((char)bytes[at])) at++;
            var start = at;
            while (!char.IsWhiteSpace((char)bytes[at])) at++;
            return Encoding.ASCII.GetString(bytes, start, at - start);
        }
        Assert.Equal("P6", Token());
        var width = int.Parse(Token());
        Token();
        Assert.Equal("255", Token());
        return new Picture(width, bytes[(at + 1)..]);
    }
}
