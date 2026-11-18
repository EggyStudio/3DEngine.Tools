using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Tools;

/// <summary>
/// The checks that a module's library is one any engine takes, each returning what it finds, so a
/// module's tests assert each empty and hold no copy of a check of their own.
/// </summary>
/// <remarks>
/// <para>
/// A module takes this repository as a submodule and references the library from its test project
/// alone, so its own library and package take nothing of the module's;
/// <see cref="Refused(IEnumerable{string})"/> refuses this assembly among the rest, so a library
/// that references it fails.
/// </para>
/// <para>
/// Each check has a form that reads a built assembly and a form that reads what it is given, the
/// second for a test that plants a fault, as this repository's tests do.
/// </para>
/// </remarks>
public static partial class ModuleChecks
{
    /// <summary>
    /// The names no reference of a module's library may carry, the engines' own assemblies, this
    /// module, and the windows, the graphics APIs and the bindings over them a .NET program draws
    /// through.
    /// </summary>
    /// <remarks>
    /// Each is matched as a whole part of a name between dots and dashes, so SDL3-CS,
    /// Silk.NET.Vulkan and Twizzle.ImGui-Bundle.NET are refused and Metadata is not. The first name
    /// refuses every assembly that carries it as its prefix, this one and the modules' among them.
    /// </remarks>
    public static IReadOnlyList<string> RefusedNames { get; } =
    [
        "3DEngine", "BevyCSharp",
        "SDL", "SDL2", "SDL3", "Glfw", "GLFW.NET", "Raylib", "Raylib-cs", "ImGui", "ImGui.NET",
        "Silk.NET", "Vortice", "Vulkan", "VulkanSharp", "OpenTK", "OpenGL", "OpenAL", "SharpDX",
        "DirectX", "Direct3D", "WebGPU", "Wgpu", "Veldrid", "SkiaSharp", "MonoGame", "FNA",
        "Stride", "Avalonia", "System.Windows.Forms", "PresentationFramework", "Microsoft.Maui",
    ];

    /// <summary>The references given that <see cref="RefusedNames"/> refuses, each once.</summary>
    public static IReadOnlyList<string> Refused(IEnumerable<string> references) =>
        references.Where(IsRefused).Distinct(StringComparer.Ordinal).ToList();

    /// <summary>
    /// The references of a built library that <see cref="RefusedNames"/> refuses, its assembly's
    /// own and the packages a dependency file records for it, followed to those they bring in turn.
    /// </summary>
    /// <param name="library">The module's library, as a test loads it.</param>
    /// <param name="dependencyFile">
    /// The test assembly's <c>.deps.json</c>, which names the library with every package the build
    /// resolved for it.
    /// </param>
    public static IReadOnlyList<string> Refused(Assembly library, string dependencyFile) =>
        Refused(library.GetReferencedAssemblies().Select(name => name.Name!).Concat(Packages(library.GetName().Name!, dependencyFile)));

    /// <summary>
    /// The packages a dependency file records for a library, followed to those they bring, by
    /// their names alone.
    /// </summary>
    /// <exception cref="ArgumentException">The file records no library of that name.</exception>
    public static IReadOnlyList<string> Packages(string library, string dependencyFile)
    {
        using var deps = JsonDocument.Parse(File.ReadAllText(dependencyFile));
        var target = deps.RootElement.GetProperty("targets").EnumerateObject().First().Value;
        var entries = target.EnumerateObject().ToDictionary(entry => entry.Name.Split('/')[0], entry => entry.Value);
        if (!entries.ContainsKey(library))
            throw new ArgumentException($"{dependencyFile} records no {library}.", nameof(library));

        var found = new SortedSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>([library]);
        while (pending.TryPop(out var name))
            if (entries.TryGetValue(name, out var entry) && entry.TryGetProperty("dependencies", out var dependencies))
                foreach (var dependency in dependencies.EnumerateObject())
                    if (found.Add(dependency.Name))
                        pending.Push(dependency.Name);
        return [.. found];
    }

    /// <summary>
    /// The public names given and the documented members that name an engine, its name or its
    /// API's prefix as a word of its own.
    /// </summary>
    /// <param name="publicNames">The library's public types and members by their full
    /// names.</param> <param name="documentation">The library's XML documentation as the build
    /// writes it.</param>
    public static IReadOnlyList<string> EngineNames(IEnumerable<string> publicNames, XDocument documentation)
    {
        // A documented member is read where it is public, its name taken from its kind's letter to
        // its parameters, since the documentation holds the private members a generator documents,
        // as a regular expression's source generator quotes its pattern.
        var names = publicNames.ToList();
        var exported = names.ToHashSet(StringComparer.Ordinal);
        var documented = documentation.Descendants("member")
            .Select(member => (Name: member.Attribute("name")!.Value, Text: member.Value))
            .Where(member => exported.Contains(member.Name[2..].Split('(')[0]));
        return names.Where(name => Engine().IsMatch(name))
            .Concat(documented.Where(member => Engine().IsMatch(member.Text)).Select(member => member.Name))
            .ToList();
    }

    /// <summary>
    /// The public types and members of a built library and the documented members that name an
    /// engine, the documentation read from the XML file beside the assembly.
    /// </summary>
    /// <remarks>
    /// The documentation's assembly element names the assembly, which carries an engine's prefix,
    /// so the members alone are read.
    /// </remarks>
    public static IReadOnlyList<string> EngineNames(Assembly library) =>
        EngineNames(PublicNames(library), XDocument.Load(Path.ChangeExtension(library.Location, ".xml")));

    /// <summary>A library's public types and their own public members, each by its full
    /// name.</summary>
    public static IReadOnlyList<string> PublicNames(Assembly library) =>
        library.GetExportedTypes().SelectMany(type =>
            type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(member => $"{type.FullName}.{member.Name}")
                .Prepend(type.FullName!))
            .ToList();

    /// <summary>
    /// The type names given that a README names neither in its section Binding it in your engine
    /// nor in a row of a table, each named in code whole, as <c>`Star`</c> or <c>`Star.Name`</c>
    /// and not as the start of <c>`Stars`</c>.
    /// </summary>
    public static IReadOnlyList<string> Unnamed(IEnumerable<string> typeNames, string readme)
    {
        var text = readme.ReplaceLineEndings("\n");
        var section = Section(text) ?? "";
        var rows = string.Join("\n", text.Split('\n').Where(line => line.StartsWith('|')));
        return typeNames.Where(name => !Named(section, name) && !Named(rows, name)).Order(StringComparer.Ordinal).ToList();
    }

    /// <summary>The public types of a built library that a README names nowhere a stranger reads
    /// first.</summary>
    public static IReadOnlyList<string> Unnamed(Assembly library, string readme) =>
        Unnamed(library.GetExportedTypes().Where(type => !type.IsNested).Select(type => type.Name).Distinct(), readme);

    /// <summary>
    /// Where the first C# block of a README's section Binding it in your engine and the lines of a
    /// test's source between its two markers differ, as a sentence, or null where they are the
    /// same.
    /// </summary>
    /// <remarks>
    /// The block begins after a line holding <c>// The README's block from here</c> and ends before
    /// one holding <c>// The end marker.</c>, its common indent taken off, so the test runs the
    /// code a stranger copies.
    /// </remarks>
    public static string? BindingBlock(string readme, string hostSource)
    {
        var section = Section(readme.ReplaceLineEndings("\n"));
        if (section is null)
            return "The README has no section Binding it in your engine.";
        var open = section.IndexOf("```csharp\n", StringComparison.Ordinal);
        if (open < 0)
            return "The README's section Binding it in your engine holds no C# block.";
        open += "```csharp\n".Length;
        var block = section[open..section.IndexOf("```\n", open, StringComparison.Ordinal)].TrimEnd('\n').Split('\n');

        var lines = hostSource.ReplaceLineEndings("\n").Split('\n');
        var from = Array.FindIndex(lines, line => line.Contains("// The README's block from here", StringComparison.Ordinal));
        var to = Array.FindIndex(lines, line => line.Contains("// The end marker.", StringComparison.Ordinal));
        if (from < 0 || to <= from)
            return "The host's source holds no lines between // The README's block from here and // The end marker.";
        var hosted = lines[(from + 1)..to];
        var indent = hosted.Where(line => line.Trim().Length > 0).Select(line => line.Length - line.TrimStart().Length).DefaultIfEmpty(0).Min();
        hosted = [.. hosted.Select(line => line.Length >= indent ? line[indent..] : line.TrimStart())];

        for (int i = 0; i < Math.Max(block.Length, hosted.Length); i++)
        {
            var (readmeLine, hostLine) = (i < block.Length ? block[i] : null, i < hosted.Length ? hosted[i] : null);
            if (readmeLine != hostLine)
                return $"Line {i + 1} of the README's block reads \"{readmeLine ?? "nothing"}\" where the host reads \"{hostLine ?? "nothing"}\".";
        }

        return null;
    }

    private static bool IsRefused(string name) =>
        RefusedNames.Any(refused => Regex.IsMatch(name, $@"(^|[.-]){Regex.Escape(refused)}($|[.-])", RegexOptions.IgnoreCase));

    private static bool Named(string text, string name) => Regex.IsMatch(text, $@"`{Regex.Escape(name)}([`.(<\[ ])");

    private static string? Section(string readme)
    {
        var start = readme.IndexOf("## Binding it in your engine", StringComparison.Ordinal);
        if (start < 0) return null;
        var end = readme.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        return readme[start..(end < 0 ? readme.Length : end)];
    }

    // Each engine's name and its API's prefix, as a word of its own.
    [GeneratedRegex(@"3DEngine|\bBevy(CSharp)?\b|\bE3D\b", RegexOptions.IgnoreCase)]
    private static partial Regex Engine();
}
