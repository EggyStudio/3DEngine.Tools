using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tools.Tests;

public partial class ChartTests
{
    [NeedsPillowFact]
    public void Each_Panel_Draws_Its_Values_Where_Its_Printed_Place_Says()
    {
        var layout = new
        {
            title = "A made sheet", subtitle = "Every kind of panel", notes = "A note under the panels.",
            columns = 2, width = 1200, panel_height = 360, font = 14,
            panels = new object[]
            {
                new
                {
                    kind = "line", title = "Linear",
                    x = new { min = 0, max = 10, ticks = new[] { 0, 10 } },
                    y = new { min = 0, max = 10, ticks = new[] { 0, 10 } },
                    bands = new[] { new { from = 7, to = 9, label = "a band" } },
                    series = new object[]
                    {
                        new { label = "solid", color = "#ff0000", points = new[] { new[] { 0.0, 5 }, new[] { 10.0, 5 } }, marked = new[] { new[] { 3.0, 5 } } },
                        new { label = "dashed", color = "#0000ff", dash = true, points = new[] { new[] { 0.0, 2 }, new[] { 10.0, 2 } } },
                    },
                },
                new
                {
                    kind = "line", title = "Logarithmic",
                    x = new { min = 0, max = 10, ticks = new[] { 0, 10 } },
                    y = new { log = true, min = 1, max = 10000 },
                    series = new object[] { new { color = "#00ff00", points = new[] { new[] { 0.0, 100 }, new[] { 10.0, 100 } } } },
                },
                new
                {
                    kind = "bars", title = "Bars",
                    x = new { min = 0, max = 40, ticks = new[] { 0, 40 } },
                    series = new[] { new { label = "first", color = "#ff0000" }, new { label = "second", color = "#00ff00" } },
                    groups = new object[] { new { label = "A", values = new double?[] { 10, 20 } }, new { label = "B", values = new double?[] { 5, null } } },
                },
                new
                {
                    kind = "table", title = "Table",
                    columns = new[] { "name", "value" },
                    rows = new[] { new[] { "Same text", "1" }, new[] { "Same text", "1" } },
                    bold = new[] { 0 },
                },
                new { kind = "map", title = "Map", scale = 1, values = new[] { new[] { 1.0, -1, 0 } } },
            },
        };
        var folder = Directory.CreateTempSubdirectory("chart-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(folder, "layout.json"), JsonSerializer.Serialize(layout));
            var printed = Programs.Script("chart.py", Path.Combine(folder, "layout.json"), Path.Combine(folder, "sheet.ppm"));
            var sheet = Picture.Read(Path.Combine(folder, "sheet.ppm"));
            var lines = Programs.Lines(printed);

            Assert.Equal((255, 255, 255), sheet.At(2, 2));
            Assert.True(sheet.Dark(0, 0, sheet.Width, 60) > 50, "the title is not drawn");
            Assert.True(sheet.Dark(0, sheet.Height - 50, sheet.Width, sheet.Height) > 20, "the notes are not drawn");

            var linear = Plot(lines, 1);
            Assert.Equal((255, 0, 0), sheet.At(linear.X(5), linear.Y(5)));
            Assert.Equal((255, 0, 0), sheet.At(linear.X(3), linear.Y(5) + 4));
            Assert.Equal((238, 236, 232), sheet.At(linear.X(2), linear.Y(8)));
            var dashed = Enumerable.Range(linear.X(1), linear.X(9) - linear.X(1)).Select(x => sheet.At(x, linear.Y(2))).ToList();
            Assert.Contains((0, 0, 255), dashed);
            Assert.Contains((255, 255, 255), dashed);

            var log = Plot(lines, 2);
            Assert.Contains(lines, line => line.StartsWith("panel 2 plot at ", StringComparison.Ordinal) && line.EndsWith(" y 1..10,000 log", StringComparison.Ordinal));
            Assert.Equal((0, 255, 0), sheet.At(log.X(5), log.Top + log.Height / 2));

            var (x0, y0, x1, y1) = Bar(lines, 3, 1, 2);
            Assert.Equal((0, 255, 0), sheet.At((x0 + x1) / 2, (y0 + y1) / 2));
            Assert.Equal((255, 255, 255), sheet.At(x1 + 3, (y0 + y1) / 2));
            Assert.DoesNotContain(lines, line => line.StartsWith("panel 3 bar 2 2 ", StringComparison.Ordinal));

            var (bold, plain) = (Row(lines, 4, 1), Row(lines, 4, 2));
            Assert.Contains(lines, line => line.StartsWith("panel 4 row 1 ", StringComparison.Ordinal) && line.EndsWith(" bold", StringComparison.Ordinal));
            Assert.True(sheet.Dark(bold.X, bold.Y, bold.X + bold.W, bold.Y + bold.H - 3) > sheet.Dark(plain.X, plain.Y, plain.X + plain.W, plain.Y + plain.H - 3),
                "the bold row is no darker than the same text plain");

            var map = Plot(lines, 5);
            var side = map.Height;
            Assert.Equal((255, 0, 0), sheet.At(map.Left + side / 2, map.Top + side / 2));
            Assert.Equal((0, 0, 255), sheet.At(map.Left + side + side / 2, map.Top + side / 2));
            Assert.Equal((0, 0, 0), sheet.At(map.Left + 2 * side + side / 2, map.Top + side / 2));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [NeedsPillowFact]
    public void A_Label_Stands_On_The_Side_Of_Its_Line_The_Layout_Gives()
    {
        var layout = new
        {
            width = 800, panel_height = 400, font = 16,
            panels = new object[]
            {
                new
                {
                    kind = "line",
                    x = new { min = 0, max = 10, ticks = new[] { 0, 10 } },
                    y = new { min = 0, max = 10, ticks = new[] { 0, 10 } },
                    series = new object[]
                    {
                        new { label = "above it", color = "#ff0000", points = new[] { new[] { 0.0, 7 }, new[] { 10.0, 7 } }, label_at = 2 },
                        new { label = "below it", color = "#0000ff", points = new[] { new[] { 0.0, 3 }, new[] { 10.0, 3 } }, label_at = 2, label_side = "below" },
                    },
                },
            },
        };
        var folder = Directory.CreateTempSubdirectory("chart-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(folder, "layout.json"), JsonSerializer.Serialize(layout));
            var lines = Programs.Lines(Programs.Script("chart.py", Path.Combine(folder, "layout.json"), Path.Combine(folder, "sheet.ppm")));
            var sheet = Picture.Read(Path.Combine(folder, "sheet.ppm"));
            var plot = Plot(lines, 1);
            int Ink(int y0, int y1) => sheet.Dark(plot.X(2), y0, plot.X(5), y1);

            Assert.True(Ink(plot.Y(7) - 30, plot.Y(7) - 4) > 30, "the label above its line is not drawn above it");
            Assert.True(Ink(plot.Y(3) + 4, plot.Y(3) + 30) > 30, "the label below its line is not drawn below it");
            Assert.True(Ink(plot.Y(3) - 30, plot.Y(3) - 4) < 5, "the label below its line is drawn above it");
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [NeedsPillowFact]
    public void A_Label_Over_A_Rising_Line_Stands_Clear_Of_It_Across_Its_Width()
    {
        var layout = new
        {
            width = 800, panel_height = 400, font = 16,
            panels = new object[]
            {
                new
                {
                    kind = "line",
                    x = new { min = 0, max = 10, ticks = new[] { 0, 10 } },
                    y = new { min = 0, max = 10, ticks = new[] { 0, 10 } },
                    series = new object[] { new { label = "a label of some width", color = "#ff0000", points = new[] { new[] { 0.0, 0 }, new[] { 10.0, 10 } }, label_at = 2 } },
                },
            },
        };
        var folder = Directory.CreateTempSubdirectory("chart-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(folder, "layout.json"), JsonSerializer.Serialize(layout));
            var lines = Programs.Lines(Programs.Script("chart.py", Path.Combine(folder, "layout.json"), Path.Combine(folder, "sheet.ppm")));
            var sheet = Picture.Read(Path.Combine(folder, "sheet.ppm"));
            var plot = Plot(lines, 1);

            // The label's ink below the line, in the plot from where it starts to the plot's end.
            int under = 0, over = 0;
            for (int x = plot.X(2); x < plot.Left + plot.Width; x++)
                for (int y = plot.Top; y < plot.Top + plot.Height; y++)
                {
                    var line = plot.Y(10.0 * (x - plot.Left) / plot.Width);
                    if (sheet.At(x, y) is not (24, 24, 24)) continue;
                    if (y > line + 2) under++;
                    else over++;
                }

            Assert.True(over > 30, "the label is not drawn");
            Assert.Equal(0, under);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [NeedsPillowFact]
    public void An_Axis_Given_Its_Range_Draws_No_Tick_Outside_It()
    {
        var layout = new
        {
            width = 800, panel_height = 400, font = 16,
            panels = new object[]
            {
                new
                {
                    // A title and a subtitle of a letter set the plot low enough for the stray tick
                    // to fall on the sheet, and stand clear of the margin read.
                    kind = "line", title = "T", subtitle = "s",
                    x = new { min = 0, max = 10 },
                    y = new { min = 20, max = 90 },
                    series = new object[] { new { color = "#ff0000", points = new[] { new[] { 0.0, 30 }, new[] { 10.0, 80 } } } },
                },
            },
        };
        var folder = Directory.CreateTempSubdirectory("chart-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(folder, "layout.json"), JsonSerializer.Serialize(layout));
            var lines = Programs.Lines(Programs.Script("chart.py", Path.Combine(folder, "layout.json"), Path.Combine(folder, "sheet.ppm")));
            var sheet = Picture.Read(Path.Combine(folder, "sheet.ppm"));
            var plot = Plot(lines, 1);

            // The round ticks of 20 to 90 run on to 100, which lies above the plot, where this
            // layout draws nothing else.
            Assert.Equal(0, sheet.Dark(plot.Left - 40, plot.Top - 60, plot.Left - 4, plot.Top - 8));
            Assert.True(sheet.Dark(plot.Left - 40, plot.Y(80) - 10, plot.Left - 4, plot.Y(80) + 10) > 10, "the tick at 80 is not drawn");
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [NeedsPillowFact]
    public void A_Panel_Of_Another_Kind_Is_Refused_By_Name()
    {
        var folder = Directory.CreateTempSubdirectory("chart-").FullName;
        try
        {
            File.WriteAllText(Path.Combine(folder, "layout.json"), "{\"panels\": [{\"kind\": \"pie\"}]}");

            var (code, printed) = Programs.Attempt("chart.py", [], Path.Combine(folder, "layout.json"), Path.Combine(folder, "sheet.png"));

            Assert.Equal(1, code);
            Assert.Contains("Panel 1 is of the kind pie, where line, bars, table and map are drawn.", printed);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    // A chart's plot as the script prints it, its place, its size and its axes' ranges, mapping a
    // value to its pixel.
    private sealed record Area(int Left, int Top, int Width, int Height, double XLow, double XHigh, double YLow, double YHigh, bool YLog)
    {
        public int X(double value) => (int)Math.Round(Left + (value - XLow) / (XHigh - XLow) * Width);

        public int Y(double value) => (int)Math.Round(Top + Height - (YLog
            ? (Math.Log10(value) - Math.Log10(YLow)) / (Math.Log10(YHigh) - Math.Log10(YLow))
            : (value - YLow) / (YHigh - YLow)) * Height);
    }

    private static Area Plot(string[] lines, int panel)
    {
        var line = lines.Single(l => l.StartsWith($"panel {panel} plot at ", StringComparison.Ordinal));
        var found = PlotLine().Match(line);
        Assert.True(found.Success, line);
        double Number(string group) => found.Groups[group].Success ? double.Parse(found.Groups[group].Value.Replace(",", ""), System.Globalization.CultureInfo.InvariantCulture) : 0;
        return new Area((int)Number("x"), (int)Number("y"), (int)Number("w"), (int)Number("h"),
            Number("xl"), Number("xh"), Number("yl"), Number("yh"), line.EndsWith(" log", StringComparison.Ordinal));
    }

    private static (int, int, int, int) Bar(string[] lines, int panel, int group, int series)
    {
        var line = lines.Single(l => l.StartsWith($"panel {panel} bar {group} {series} at ", StringComparison.Ordinal));
        var numbers = Regex.Matches(line, @"-?\d+").Select(m => int.Parse(m.Value)).ToArray();
        return (numbers[3], numbers[4], numbers[5], numbers[6]);
    }

    private static (int X, int Y, int W, int H) Row(string[] lines, int panel, int row)
    {
        var line = lines.Single(l => l.StartsWith($"panel {panel} row {row} at ", StringComparison.Ordinal));
        var numbers = Regex.Matches(line, @"\d+").Select(m => int.Parse(m.Value)).ToArray();
        return (numbers[2], numbers[3], numbers[4], numbers[5]);
    }

    [GeneratedRegex(@"plot at (?<x>\d+),(?<y>\d+) size (?<w>\d+)x(?<h>\d+)( x (?<xl>[\d,.]+)\.\.(?<xh>[\d,.]+)( log)?( y (?<yl>[\d,.]+)\.\.(?<yh>[\d,.]+))?)?")]
    private static partial Regex PlotLine();

    private sealed record Picture(int Width, int Height, byte[] Pixels)
    {
        public (int, int, int) At(int x, int y)
        {
            var i = 3 * (y * Width + x);
            return (Pixels[i], Pixels[i + 1], Pixels[i + 2]);
        }

        public int Dark(int x0, int y0, int x1, int y1)
        {
            int count = 0;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    if (At(x, y) is var (r, g, b) && r + g + b < 300) count++;
            return count;
        }

        // Pillow writes a binary PPM's header as three lines, P6, the size and the largest value.
        public static Picture Read(string path)
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
            var height = int.Parse(Token());
            Assert.Equal("255", Token());
            return new Picture(width, height, bytes[(at + 1)..]);
        }
    }
}
