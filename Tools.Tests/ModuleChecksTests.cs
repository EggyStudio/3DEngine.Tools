using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace Tools.Tests;

/// <summary>
/// Each of the module's checks finds a fault planted for it and passes this repository's own
/// library, and the README's block a module copies runs here word for word.
/// </summary>
public class ModuleChecksTests
{
    // The three a module sets, its library's assembly name, its checkout's root and the test whose
    // host runs the README's binding block, here this repository's own.
    private const string Library = "3DEngine.Tools";
    private static readonly string Root = Programs.Root();
    private const string Host = "Tools.Tests/ModuleChecksTests.cs";

    // The README's block from here to the end marker, word for word.
    private static readonly string Deps = Path.Combine(AppContext.BaseDirectory, Assembly.GetExecutingAssembly().GetName().Name + ".deps.json");
    private static readonly string Readme = File.ReadAllText(Path.Combine(Root, "README.md"));

    [Fact]
    public void The_Library_Takes_No_Engine_Window_Or_Graphics_Api() =>
        Assert.Null(ModuleChecks.ReferenceFault(Assembly.Load(Library), Deps));

    [Fact]
    public void No_Public_Name_Or_Its_Documentation_Names_An_Engine() =>
        Assert.Null(ModuleChecks.EngineNameFault(Assembly.Load(Library)));

    [Fact]
    public void The_Readme_Names_Every_Public_Type() =>
        Assert.Null(ModuleChecks.UnnamedFault(Assembly.Load(Library), Readme));

    [Fact]
    public void The_Readmes_Binding_Block_Stands_Word_For_Word_In_The_Host() =>
        Assert.Null(ModuleChecks.BindingBlock(Readme, File.ReadAllText(Path.Combine(Root, Host))));
    // The end marker.

    [Fact]
    public void Each_Fault_Names_What_It_Found_The_Rule_And_What_To_Do()
    {
        var documentation = XDocument.Parse("<doc><members><member name=\"T:Sample.Scene\"><summary>Drawn by 3DEngine.</summary></member></members></doc>");

        Assert.Null(ModuleChecks.ReferenceFault(["System.Runtime"]));
        Assert.Equal("The library references Vortice.Vulkan, an engine, a window or a graphics API, which a module's library takes none of (NORM.md, N 2.1). "
            + "Remove the reference, or name it in allowed where it is a module the library may take.", ModuleChecks.ReferenceFault(["Vortice.Vulkan", "System.Runtime"]));
        Assert.Null(ModuleChecks.EngineNameFault(["Sample.Scene"], XDocument.Parse("<doc><members/></doc>")));
        Assert.StartsWith("T:Sample.Scene name an engine in the library's public names or their documentation", ModuleChecks.EngineNameFault(["Sample.Scene"], documentation));
        Assert.EndsWith("Say what the engine's name stood for in the module's own terms.", ModuleChecks.EngineNameFault(["Sample.Scene"], documentation));
        Assert.Null(ModuleChecks.UnnamedFault(["Star"], "## Binding it in your engine\n\nA `Star`.\n"));
        Assert.StartsWith("The README names Moon neither in its section Binding it in your engine nor in a table's row", ModuleChecks.UnnamedFault(["Star", "Moon"], "## Binding it in your engine\n\nA `Star`.\n"));
    }

    [Theory]
    [InlineData("3DEngine", true)]
    [InlineData("3DEngine.Tools", true)]
    [InlineData("BevyCSharp", true)]
    [InlineData("SDL3-CS", true)]
    [InlineData("Vortice.Vulkan", true)]
    [InlineData("Twizzle.ImGui-Bundle.NET", true)]
    [InlineData("Silk.NET.Vulkan", true)]
    [InlineData("Raylib-cs", true)]
    [InlineData("System.Runtime", false)]
    [InlineData("System.Text.RegularExpressions", false)]
    [InlineData("System.Reflection.Metadata", false)]
    [InlineData("xunit.core", false)]
    public void A_Reference_Is_Refused_Where_A_Part_Of_Its_Name_Is_A_Refused_Name(string name, bool refused)
    {
        string[] expected = refused ? [name] : [];
        Assert.Equal(expected, ModuleChecks.Refused([name]));
    }

    [Fact]
    public void A_Module_Allowed_By_Its_Whole_Name_Passes_And_Its_Neighbors_Are_Still_Refused()
    {
        string[] references = ["3DEngine.Modeling", "3DEngine.Modeling.Editor", "3DEngine.Levels", "3DEngine", "System.Runtime"];

        Assert.Equal(["3DEngine.Modeling", "3DEngine.Modeling.Editor", "3DEngine.Levels", "3DEngine"], ModuleChecks.Refused(references));
        Assert.Equal(["3DEngine.Modeling.Editor", "3DEngine.Levels", "3DEngine"], ModuleChecks.Refused(references, allowed: ["3DEngine.Modeling"]));
    }

    [Fact]
    public void A_Package_A_Library_Brings_Through_Another_Is_Read_From_The_Dependency_File()
    {
        // A library that takes a package, which takes a binding of Vulkan in turn, one that takes
        // this repository, and a test project that takes this repository, which is no package of
        // either library's.
        var file = Path.Combine(Directory.CreateTempSubdirectory("deps-").FullName, "Sample.Tests.deps.json");
        File.WriteAllText(file, """
            { "targets": { ".NETCoreApp,Version=v10.0": {
                "Sample.Tests/1.0.0": { "dependencies": { "Sample": "1.0.0", "3DEngine.Tools": "1.0.0" } },
                "Sample/1.0.0": { "dependencies": { "Meshes": "2.0.0" } },
                "Taking/1.0.0": { "dependencies": { "3DEngine.Tools": "1.0.0" } },
                "Meshes/2.0.0": { "dependencies": { "Silk.NET.Vulkan": "2.21.0" } },
                "Silk.NET.Vulkan/2.21.0": {},
                "3DEngine.Tools/1.0.0": {}
            } } }
            """);
        try
        {
            Assert.Equal(["Meshes", "Silk.NET.Vulkan"], ModuleChecks.Packages("Sample", file));
            Assert.Equal(["Silk.NET.Vulkan"], ModuleChecks.Refused(ModuleChecks.Packages("Sample", file)));
            Assert.Throws<ArgumentException>(() => ModuleChecks.Packages("Other", file));

            // A library that takes this repository itself is refused, where the tests that take it
            // are not read.
            Assert.Equal(["3DEngine.Tools"], ModuleChecks.Refused(ModuleChecks.Packages("Taking", file)));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(file)!, true);
        }
    }

    [Fact]
    public void A_Public_Name_And_A_Summary_That_Name_An_Engine_Are_Found()
    {
        var documentation = XDocument.Parse("""
            <doc><assembly><name>3DEngine.Sample</name></assembly><members>
                <member name="T:Sample.Scene"><summary>A scene as the engine draws it.</summary></member>
                <member name="M:Sample.Scene.Load(System.String)"><summary>Loads a scene as 3DEngine's loader does.</summary></member>
                <member name="M:Sample.Scene.Pattern"><remarks>A private pattern, 3DEngine|Bevy.</remarks></member>
            </members></doc>
            """);

        var found = ModuleChecks.EngineNames(["Sample.Scene", "Sample.Scene.Load", "Sample.BevyBridge", "Sample.Bevy"], documentation);

        // The private member a generator documented is no part of the public surface.
        Assert.Equal(["Sample.Bevy", "M:Sample.Scene.Load(System.String)"], found);
    }

    [Fact]
    public void A_Public_Type_The_Readme_Names_Nowhere_A_Stranger_Reads_First_Is_Found()
    {
        var readme = """
            # Sample

            `Sun` is named here, outside the section.

            ## Binding it in your engine

            Make a `Star` and read `Star.Name`.

            | type | what it is |
            |---|---|
            | `Moon` | the moon |

            ## Building
            """;

        Assert.Equal(["Stars", "Sun"], ModuleChecks.Unnamed(["Star", "Moon", "Stars", "Sun"], readme));
    }

    [Fact]
    public void A_Readme_Block_That_Differs_From_Its_Host_Is_Named_By_Its_Line()
    {
        var readme = "# Sample\n\n## Binding it in your engine\n\n```csharp\nvar a = 1;\nvar b = 2;\n```\n";
        string Host(string second) => $"class T\n{{\n    void M()\n    {{\n        // The README's block from here\n        var a = 1;\n        {second}\n        // The end marker.\n    }}\n}}\n";

        Assert.Null(ModuleChecks.BindingBlock(readme, Host("var b = 2;")));
        Assert.StartsWith("Line 2 of the README's block reads \"var b = 2;\" where the host reads \"var b = 3;\". ", ModuleChecks.BindingBlock(readme, Host("var b = 3;")));
        Assert.Equal("The README has no section Binding it in your engine.", ModuleChecks.BindingBlock("# Sample\n", Host("var b = 2;")));
        Assert.NotNull(ModuleChecks.BindingBlock(readme, "class T { }\n"));
    }

    [Fact]
    public void A_Module_Built_Without_The_Submodule_Stops_With_One_Sentence_Naming_The_Command()
    {
        // A module's test project as the README gives its items, built with no Tools beside it.
        var readme = File.ReadAllText(Path.Combine(Root, "README.md")).ReplaceLineEndings("\n");
        var section = readme.IndexOf("### A module's checks", StringComparison.Ordinal);
        var open = readme.IndexOf("```xml\n", section, StringComparison.Ordinal) + "```xml\n".Length;
        var items = readme[open..readme.IndexOf("```\n", open, StringComparison.Ordinal)];
        var module = Directory.CreateTempSubdirectory("module-").FullName;
        var tests = Path.Combine(module, "Sample.Tests");
        Directory.CreateDirectory(tests);
        File.WriteAllText(Path.Combine(tests, "Sample.Tests.csproj"),
            $"<Project Sdk=\"Microsoft.NET.Sdk\">\n<PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>\n{items}</Project>\n");
        File.WriteAllText(Path.Combine(tests, "Checks.cs"), "class Checks { object M() => Tools.ModuleChecks.RefusedNames; }\n");
        try
        {
            var (code, printed) = Programs.Build(tests);

            Assert.NotEqual(0, code);
            Assert.Contains("The Tools submodule is not fetched. Run git submodule update --init modules/Tools.", printed);
            Assert.DoesNotContain("error CS", printed);
            Assert.DoesNotContain("error NU", printed);
        }
        finally
        {
            Directory.Delete(module, true);
        }
    }
}
