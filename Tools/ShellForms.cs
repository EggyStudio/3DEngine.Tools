using System.Text.RegularExpressions;

namespace Tools;

/// <summary>
/// A form of shell that GNU's tools or bash 4 read and macOS's BSD tools or bash 3.2 do not.
/// </summary>
/// <param name="Name">The form as a script writes it, such as <c>sed -i</c>.</param>
/// <param name="Pattern">How the form is found in a line of code.</param>
/// <param name="Instead">What reads alike on both systems in its place.</param>
public sealed record ShellForm(string Name, Regex Pattern, string Instead);

/// <summary>A line of a script that holds a <see cref="ShellForm"/>.</summary>
/// <param name="Script">
/// The script by its path from the checkout's root, with forward slashes.
/// </param>
/// <param name="Line">The line's number, from 1.</param>
/// <param name="Form">The form the line holds.</param>
public sealed record ShellFinding(string Script, int Line, ShellForm Form)
{
    /// <summary>
    /// The finding as a test's message gives it, the place, the form and what to write instead.
    /// </summary>
    public override string ToString() => $"{Script}:{Line} has {Form.Name}, where {Form.Instead} reads on both";
}

/// <summary>
/// The shell forms only GNU's tools or bash 4 read, and the scripts of a checkout read for them, as
/// N 6.6 of NORM.md asks of a script more than one system runs.
/// </summary>
/// <remarks>
/// <para>
/// A script written on Linux passes every Linux run and fails the first on macOS on such a form, as
/// <c>sed -i</c> with no suffix did in an engine's pack, whose BSD reading took the first file's
/// name as its script. Windows runs scripts in Git's bash, whose tools are GNU's, so macOS is the
/// system that tells, and a test reads for it on every system.
/// </para>
/// <para>
/// An engine's test calls <see cref="Find"/> over <see cref="Scripts"/> or
/// <see cref="RunElsewhere"/> with its root and asserts nothing is found.
/// </para>
/// </remarks>
public static partial class ShellForms
{
    /// <summary>Every form the scripts are read for.</summary>
    public static IReadOnlyList<ShellForm> All { get; } =
    [
        new("sed -i", new(@"\bsed\s+(?:-\w+\s+)*-\w*i\b"), "sed writing a file beside it, moved over it"),
        new("grep -P", new(@"\bgrep\s+(?:-\w+\s+)*-\w*P"), "grep -E, or Python"),
        new("readarray", new(@"\b(?:readarray|mapfile)\b"), "a while read loop"),
        new("date -d", new(@"\bdate\s+(?:\S+\s+)*?(?:-d|--date)\b"), "Python"),
        new("stat -c", new(@"\bstat\s+(?:-\w+\s+)*(?:-c|--format)\b"), "wc -c, or Python"),
        new("sha256sum", new(@"\b(?:sha256sum|sha1sum|md5sum)\b"), "Python's hashlib"),
        new("${x,,}", new(@"\$\{[A-Za-z_]\w*(?:,,?|\^\^?)\}"), "tr"),
        new("declare -A", new(@"\b(?:declare|local|typeset)\s+(?:-\w+\s+)*-\w*A\b"), "a list of pairs, or a case"),
        new("|&", new(@"\|&"), "2>&1 |"),
        new("&>>", new(@"&>>"), ">> file 2>&1"),
    ];

    // The folders a build, a fetch or a tool fills, read in no checkout.
    private static readonly HashSet<string> Filled = new(StringComparer.Ordinal) { ".git", ".ref", "bin", "obj", "node_modules" };

    /// <summary>The forms a line holds, its comment left out.</summary>
    public static IEnumerable<ShellForm> In(string line)
    {
        var code = Code(line);
        return All.Where(form => form.Pattern.IsMatch(code));
    }

    /// <summary>A line without its comment.</summary>
    /// <remarks>
    /// A script that names a form to say it is left out is not taken as using it. A <c>#</c> inside
    /// <c>${...}</c> counts a length and begins no comment, so a comment begins only at a line's
    /// start or after a space.
    /// </remarks>
    public static string Code(string line) => Comment().Replace(line, "");

    /// <summary>Every line of the scripts given that holds a form.</summary>
    /// <param name="root">The checkout's root.</param>
    /// <param name="scripts">The scripts by their paths from the root.</param>
    public static IReadOnlyList<ShellFinding> Find(string root, IEnumerable<string> scripts)
    {
        var found = new List<ShellFinding>();
        foreach (var script in scripts)
        {
            var lines = File.ReadAllLines(Path.Combine(root, script));
            for (int i = 0; i < lines.Length; i++)
                foreach (var form in In(lines[i]))
                    found.Add(new ShellFinding(script, i + 1, form));
        }

        return found;
    }

    /// <summary>
    /// Every shell script of a checkout, each file ending in <c>.sh</c>, by its path from the root
    /// with forward slashes.
    /// </summary>
    /// <param name="root">The checkout's root.</param>
    /// <param name="leftOut">
    /// Folders by their paths from the root whose scripts a build or a fetch leaves, besides
    /// <c>.git</c>, <c>.ref</c>, <c>bin</c>, <c>obj</c> and <c>node_modules</c>, which are left out
    /// wherever they lie, and the submodules <c>.gitmodules</c> names and any folder holding a
    /// <c>.git</c> of its own, which are other repositories'.
    /// </param>
    public static SortedSet<string> Scripts(string root, params string[] leftOut)
    {
        var skipped = leftOut.Select(folder => folder.Replace('\\', '/').TrimEnd('/')).Concat(Submodules(root)).ToHashSet(StringComparer.Ordinal);
        var scripts = new SortedSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>([root]);
        while (pending.TryPop(out var folder))
        {
            foreach (var file in Directory.EnumerateFiles(folder, "*.sh"))
                scripts.Add(Relative(root, file));
            // A folder holding a .git of its own is another repository's, a submodule's or one
            // nested inside the checkout, whose scripts are read in its own checkout.
            foreach (var child in Directory.EnumerateDirectories(folder))
                if (!Filled.Contains(Path.GetFileName(child)) && !skipped.Contains(Relative(root, child)) && !Path.Exists(Path.Combine(child, ".git")))
                    pending.Push(child);
        }

        return scripts;
    }

    /// <summary>
    /// The scripts the jobs of a checkout's workflows run on any system but Ubuntu name, and those
    /// each of them names in turn, by their paths from the root.
    /// </summary>
    /// <remarks>
    /// A job whose <c>runs-on</c> comes from a matrix counts as running elsewhere, since the matrix
    /// decides that and not the job. A script is named by its path from the root, with or without
    /// <c>./</c>, and only a file of the checkout counts, so a machine's local settings a script
    /// reads where it has them are left out.
    /// </remarks>
    /// <param name="root">The checkout's root.</param>
    /// <param name="launchers">
    /// Programs of the checkout's root with no <c>.sh</c> that the jobs or the scripts run, such as
    /// an engine's command line.
    /// </param>
    public static SortedSet<string> RunElsewhere(string root, params string[] launchers)
    {
        var pending = new Stack<string>();
        var workflows = Path.Combine(root, ".github", "workflows");
        if (Directory.Exists(workflows))
            foreach (var workflow in Directory.EnumerateFiles(workflows, "*.yml"))
                foreach (var job in Job().Split(File.ReadAllText(workflow).ReplaceLineEndings("\n")))
                    if (Elsewhere().IsMatch(job))
                        foreach (var script in Called(root, job, launchers))
                            pending.Push(script);

        var scripts = new SortedSet<string>(StringComparer.Ordinal);
        while (pending.TryPop(out var script))
            if (scripts.Add(script))
                foreach (var line in File.ReadLines(Path.Combine(root, script)))
                    foreach (var called in Called(root, Code(line), launchers))
                        pending.Push(called);
        return scripts;
    }

    private static IEnumerable<string> Called(string root, string text, string[] launchers)
    {
        var names = Named().Matches(text).Select(match => match.Value);
        foreach (var launcher in launchers)
            names = names.Concat(Regex.Matches(text, $@"(?<![\w/.-])(?:\./)?{Regex.Escape(launcher)}(?![\w.-])").Select(match => match.Value));
        return names
            .Select(name => name.StartsWith("./", StringComparison.Ordinal) ? name[2..] : name)
            .Where(name => File.Exists(Path.Combine(root, name)));
    }

    // The paths .gitmodules gives a checkout's submodules, with forward slashes.
    private static IEnumerable<string> Submodules(string root)
    {
        var file = Path.Combine(root, ".gitmodules");
        return File.Exists(file)
            ? File.ReadLines(file).Select(line => SubmodulePath().Match(line)).Where(match => match.Success)
                .Select(match => match.Groups[1].Value.Trim().Replace('\\', '/').TrimEnd('/'))
            : [];
    }

    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');

    [GeneratedRegex(@"^\s*path\s*=\s*(.+)$")]
    private static partial Regex SubmodulePath();

    [GeneratedRegex(@"(^|\s)#.*")]
    private static partial Regex Comment();

    [GeneratedRegex(@"^  [\w-]+:\n", RegexOptions.Multiline)]
    private static partial Regex Job();

    [GeneratedRegex(@"^\s+runs-on:\s*(?!ubuntu-)\S", RegexOptions.Multiline)]
    private static partial Regex Elsewhere();

    [GeneratedRegex(@"(?<![\w/.-])(?:\./)?(?:[\w.-]+/)*[\w.-]+\.sh\b")]
    private static partial Regex Named();
}
