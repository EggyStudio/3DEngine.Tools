using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Xunit;

namespace Tools.Tests;

/// <summary>
/// Skipped where no Python 3 runs, so a run's summary says the script's tests did not run.
/// </summary>
public sealed class NeedsPythonFactAttribute : FactAttribute
{
    public NeedsPythonFactAttribute()
    {
        if (Programs.Python.Value is null) Skip = "No Python 3 runs here as python3 or python.";
    }
}

/// <summary>
/// Skipped where no Python 3 runs or the one that does has no Pillow, which a script needs to read
/// or write a picture.
/// </summary>
public sealed class NeedsPillowFactAttribute : FactAttribute
{
    public NeedsPillowFactAttribute()
    {
        if (Programs.Python.Value is null) Skip = "No Python 3 runs here as python3 or python.";
        else if (!Programs.Pillow.Value) Skip = $"{Programs.Python.Value} has no Pillow, which the sheet needs.";
    }
}

/// <summary>
/// Skipped where no Python 3 runs or on Windows, which makes a symbolic link only in developer mode
/// or for an administrator, so a test of a path reached through one runs on Linux and macOS.
/// </summary>
public sealed class NeedsPythonAndSymbolicLinksFactAttribute : FactAttribute
{
    public NeedsPythonAndSymbolicLinksFactAttribute()
    {
        if (Programs.Python.Value is null) Skip = "No Python 3 runs here as python3 or python.";
        else if (OperatingSystem.IsWindows()) Skip = "Windows makes a symbolic link only in developer mode or for an administrator.";
    }
}

/// <summary>Runs the module's scripts and the programs they call.</summary>
public static class Programs
{
    /// <summary>
    /// The command that runs Python 3, <c>python3</c> as Linux and macOS name it, or <c>python</c>
    /// as Windows does, or null where neither runs.
    /// </summary>
    public static readonly Lazy<string?> Python = new(() => new[] { "python3", "python" }.FirstOrDefault(Runs));

    /// <summary>
    /// Whether the Python that runs imports Pillow, in isolated mode as the scripts run.
    /// </summary>
    public static readonly Lazy<bool> Pillow = new(() => Python.Value is { } python && Exits(python, "-I", "-c", "import PIL"));

    /// <summary>
    /// The lines a program printed, split at any line end, since a program on Windows ends its
    /// lines with a carriage return before the line feed.
    /// </summary>
    public static string[] Lines(string printed) => printed.ReplaceLineEndings("\n").Split('\n');

    /// <summary>
    /// The environment that gives a script a stdout of a single-byte encoding, Latin-1 on Linux,
    /// where a script that prints what it found as it is meets the encoding Windows gives a pipe.
    /// Elsewhere none, since Windows gives one of its own and macOS's Python writes UTF-8.
    /// </summary>
    public static Dictionary<string, string?> SingleByteLocale() =>
        OperatingSystem.IsLinux() ? new() { ["LC_ALL"] = "en_US.ISO-8859-1" } : [];

    /// <summary>The checkout's root, the folder above the tests that holds Tools.slnx.</summary>
    public static string Root()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
            if (File.Exists(Path.Combine(folder.FullName, "Tools.slnx"))) return folder.FullName;
        throw new DirectoryNotFoundException($"No folder above {AppContext.BaseDirectory} holds Tools.slnx.");
    }

    /// <summary>
    /// Runs a script copied beside the tests with Python's isolated mode, as a repository runs it,
    /// and returns what it printed.
    /// </summary>
    public static string Script(string name, params string[] arguments) => Script(name, [], arguments);

    /// <summary>
    /// Runs a script as <see cref="Script(string, string[])"/> does, with variables set in its
    /// environment.
    /// </summary>
    public static string Script(string name, Dictionary<string, string> environment, params string[] arguments) =>
        Run(Python.Value!, AppContext.BaseDirectory, environment, ["-I", Path.Combine(AppContext.BaseDirectory, "scripts", name), .. arguments]);

    /// <summary>
    /// Runs a program in a folder and returns what it printed, failing the test where it exits with
    /// an error.
    /// </summary>
    public static string Run(string program, string folder, params string[] arguments) => Run(program, folder, [], arguments);

    /// <summary>
    /// Runs a script as <see cref="Script(string, string[])"/> does and returns its exit code with
    /// what it printed and what it wrote as errors, for a script whose failing a test reads. A
    /// variable given as null is taken out of the script's environment.
    /// </summary>
    public static (int Code, string Output) Attempt(string name, Dictionary<string, string?> environment, params string[] arguments) =>
        Start(Python.Value!, AppContext.BaseDirectory, environment, ["-I", Path.Combine(AppContext.BaseDirectory, "scripts", name), .. arguments]) is var (code, output, error)
            ? (code, output + error)
            : default;

    /// <summary>
    /// Builds the project in a folder with the SDK that runs the tests and returns its exit code
    /// with what it printed, for a test whose build's failing is what it reads.
    /// </summary>
    /// <remarks>
    /// The variables a test run inherits from the MSBuild that started it are taken out, so the
    /// build is one a contributor starts, and MSBuild's nodes end with it.
    /// </remarks>
    public static (int Code, string Output) Build(string folder) => Dotnet(folder, "build", "-nologo");

    /// <summary>
    /// Runs the SDK's command given in a folder as <see cref="Build"/> builds there, and returns
    /// its exit code with what it printed.
    /// </summary>
    public static (int Code, string Output) Dotnet(string folder, params string[] arguments)
    {
        var environment = Environment.GetEnvironmentVariables().Keys.Cast<string>()
            .Where(key => key.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase) || key.StartsWith("VSTEST", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(key => key, string? (_) => null);
        environment["MSBUILDDISABLENODEREUSE"] = "1";
        environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        var (code, output, error) = Start("dotnet", folder, environment, arguments);
        return (code, output + error);
    }

    /// <summary>
    /// Runs a program in a folder with variables set in its environment, null ones taken out, and
    /// returns its exit code with what it printed and wrote as errors, for a test whose program
    /// runs another, as a commit runs its hook.
    /// </summary>
    public static (int Code, string Output) Execute(string program, string folder, Dictionary<string, string?> environment, params string[] arguments) =>
        Start(program, folder, environment, arguments) is var (code, output, error) ? (code, output + error) : default;

    private static string Run(string program, string folder, Dictionary<string, string> environment, string[] arguments)
    {
        var (code, output, error) = Start(program, folder, environment.ToDictionary(pair => pair.Key, pair => (string?)pair.Value), arguments);
        Assert.True(code == 0, $"{program} {string.Join(' ', arguments)} exited with {code}. {error}");
        return output;
    }

    private static (int Code, string Output, string Error) Start(string program, string folder, Dictionary<string, string?> environment, string[] arguments)
    {
        // Read as UTF-8, which the scripts write, where Windows would read a pipe in its console's
        // code page.
        var start = new ProcessStartInfo(program)
        {
            WorkingDirectory = folder, RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        foreach (var (key, value) in environment)
            if (value is null) start.Environment.Remove(key);
            else start.Environment[key] = value;
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output, error.Result);
    }

    private static bool Exits(string program, params string[] arguments)
    {
        var start = new ProcessStartInfo(program) { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0;
    }

    // Windows names a Store installer python3 where no Python is installed, which prints where to
    // get one and exits with an error, so the version it prints is read rather than its being
    // found.
    private static bool Runs(string command)
    {
        try
        {
            var start = new ProcessStartInfo(command, "--version") { RedirectStandardOutput = true, RedirectStandardError = true };
            using var process = Process.Start(start)!;
            var printed = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 && printed.StartsWith("Python 3", StringComparison.Ordinal);
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
}
