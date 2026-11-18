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
    public void The_Library_Takes_No_Engine_Window_Or_Graphics_Api()
    {
        var fault = ModuleChecks.ReferenceFault(Assembly.Load(Library), Deps);
        Assert.True(fault is null, fault);
    }

    [Fact]
    public void No_Public_Name_Or_Its_Documentation_Names_An_Engine()
    {
        var fault = ModuleChecks.EngineNameFault(Assembly.Load(Library));
        Assert.True(fault is null, fault);
    }

    [Fact]
    public void The_Readme_Names_Every_Public_Type()
    {
        var fault = ModuleChecks.UnnamedFault(Assembly.Load(Library), Readme);
        Assert.True(fault is null, fault);
    }

    [Fact]
    public void The_Readmes_Binding_Block_Stands_Word_For_Word_In_The_Host()
    {
        var fault = ModuleChecks.BindingBlock(Readme, File.ReadAllText(Path.Combine(Root, Host)));
        Assert.True(fault is null, fault);
    }

    [Fact]
    public void The_Readme_Opens_With_Its_Link_Row()
    {
        var fault = ModuleChecks.LinkRowFault(Assembly.Load(Library), Readme);
        Assert.True(fault is null, fault);
    }
    // The end marker.

    [Fact]
    public void Each_Fault_Names_What_It_Found_The_Rule_And_What_To_Do()
    {
        var documentation = XDocument.Parse("<doc><members><member name=\"T:Sample.Scene\"><summary>Drawn by 3DEngine.</summary></member></members></doc>");

        Assert.Null(ModuleChecks.ReferenceFault(["System.Runtime"]));
        Assert.Equal("The library references Vortice.Vulkan, an engine, a window or a graphics API, which a module's library takes none of (NORM.md, N 2.1). "
            + "Remove the reference, or name it in allowed where it is a module the library may take.", ModuleChecks.ReferenceFault(["Vortice.Vulkan", "System.Runtime"]));
        Assert.Null(ModuleChecks.EngineNameFault(["Sample.Scene"], XDocument.Parse("<doc><members/></doc>")));
        Assert.StartsWith("T:Sample.Scene names an engine in the library's public names or their documentation", ModuleChecks.EngineNameFault(["Sample.Scene"], documentation));
        Assert.StartsWith("Sample.Bevy, T:Sample.Scene name an engine", ModuleChecks.EngineNameFault(["Sample.Scene", "Sample.Bevy"], documentation));
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
    public void A_Generic_Type_Is_Named_By_Its_Plain_Name_And_Its_Parameters()
    {
        var readme = "# Sample\n\n## Binding it in your engine\n\nA `SceneFieldPlan<TCell, TLight>` plans the field.\n";

        Assert.Empty(ModuleChecks.Unnamed(["SceneFieldPlan`2"], readme));
        Assert.Null(ModuleChecks.UnnamedFault(["SceneFieldPlan`2"], readme));
        Assert.StartsWith("The README names SceneField neither", ModuleChecks.UnnamedFault(["SceneField`1"], readme));
    }

    [Fact]
    public void A_Modules_Tracked_Text_That_Names_An_Engine_Is_Found_Outside_Its_Assembly_Name()
    {
        var repo = Directory.CreateTempSubdirectory("module-").FullName;
        try
        {
            // The assembly's name of the form the rule keeps, the review's and the norm's files and
            // the module's own submodule left out, an untracked file read by no one, and two lines
            // that name an engine where a module names none.
            Write(repo, "Sample/Sample.csproj", "<Project><PropertyGroup><AssemblyName>3DEngine.Sample</AssemblyName></PropertyGroup></Project>\n");
            Write(repo, "README.md", "# Sample\n\nA module any engine takes.\nIt was drawn in BevyCSharp first.\n");
            Write(repo, "Sample/Scene.cs", "namespace Sample;\n// The scene as 3DEngine's renderer draws it.\npublic class Scene { }\n");
            Write(repo, ".github/REVIEW.md", "3DEngine's item 136.\n");
            Write(repo, ".github/NORM.md", "The engines, 3DEngine and BevyCSharp.\n");
            Write(repo, "modules/Tools/README.md", "Both engines, 3DEngine and BevyCSharp.\n");
            Programs.Run("git", repo, "init", "-q");
            Programs.Run("git", repo, "add", "-A");
            Write(repo, "notes.md", "Bevy\n");

            Assert.Equal(["README.md:4", "Sample/Scene.cs:2"], ModuleChecks.EngineNamesInText(repo));
            Assert.Equal("README.md:4 names an engine, \"BevyCSharp\", where a module names none outside its assembly's name of the form 3DEngine.<Module>, "
                + "and 1 line more does. Say it in the module's own terms, as any engine reads them.", ModuleChecks.EngineNameInTextFault(repo));
            File.WriteAllText(Path.Combine(repo, "README.md"), "# Sample\n");
            File.WriteAllText(Path.Combine(repo, "Sample/Scene.cs"), "namespace Sample;\npublic class Scene { }\n");
            Assert.Null(ModuleChecks.EngineNameInTextFault(repo));
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(repo, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(repo, true);
        }
    }

    [Fact]
    public void Another_Product_In_A_Modules_Text_Is_Listed_Outside_A_License_Or_A_Citation()
    {
        var repo = Directory.CreateTempSubdirectory("module-").FullName;
        try
        {
            Write(repo, "README.md", "# Sample\n\nThe sky as Unity lights it.\nThe air after HDRP's model (2019).\nSee https://example.com/godot for more.\n");
            Write(repo, "LICENSE", "Parts by Unity Technologies.\n");
            Write(repo, "THIRD-PARTY-NOTICES.md", "Godot's shaders, under its license.\n");
            Write(repo, "Sample/Sky.cs", "namespace Sample;\n// The haze as Unreal draws it.\npublic class Sky { }\n");
            Programs.Run("git", repo, "init", "-q");
            Programs.Run("git", repo, "add", "-A");

            Assert.Equal(["README.md:3", "Sample/Sky.cs:2"], ModuleChecks.ProductNamesInText(repo));
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(repo, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(repo, true);
        }
    }

    [Fact]
    public void A_Readme_Opens_With_Its_Link_Row_Above_Its_Title()
    {
        const string row = "[Devlog](https://github.com/EggyStudio/Sample/blob/main/DEVLOG.md) [Cheatsheet](https://github.com/EggyStudio/Sample/blob/main/CHEATSHEET.md)";

        Assert.Null(ModuleChecks.LinkRowFault("Sample", $"{row}\n\n# Sample\n\nA module.\n"));
        Assert.Null(ModuleChecks.LinkRowFault("Sample", $"{row}\r\n\r\n# Sample\r\n"));
        Assert.StartsWith("The README opens with \"# Sample\" where a module's README opens with its link row", ModuleChecks.LinkRowFault("Sample", $"# Sample\n\n{row}\n"));
        Assert.NotNull(ModuleChecks.LinkRowFault("Sample", row.Replace(") [", ") \u00b7 [", StringComparison.Ordinal) + "\n\n# Sample\n"));
        Assert.NotNull(ModuleChecks.LinkRowFault("Sample", "[Devlog](https://github.com/EggyStudio/Sample/blob/main/DEVLOG.md)\n\n# Sample\n"));
        Assert.NotNull(ModuleChecks.LinkRowFault("Sample", $"{row}\n# Sample\n"));
        Assert.NotNull(ModuleChecks.LinkRowFault("Sample", $"{row}\n\n# Other\n"));
        Assert.EndsWith($"\"{row}\", a blank line and its title \"# Sample\". Put the row there, its links apart by a space.", ModuleChecks.LinkRowFault("Sample", "# Sample\n"));
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
    public void A_Module_Taking_The_Checks_By_The_Readme_Fails_Each_With_Its_Sentence_Whole()
    {
        // A module laid out as the README asks, this repository's library copied as its submodule,
        // its test project the README's items and its test the README's four facts, whose library
        // breaks every check: it references a binding named for SDL3, a summary names an engine, a
        // public type stands in no table and its README's block is no text of the host's.
        var readme = File.ReadAllText(Path.Combine(Root, "README.md")).ReplaceLineEndings("\n");
        var section = readme.IndexOf("### A module's checks", StringComparison.Ordinal);
        var items = Between(readme, "```xml\n", section);
        var facts = Between(readme, "```csharp\n", section);
        var module = Directory.CreateTempSubdirectory("module-").FullName;
        try
        {
            var tools = Path.Combine(module, "modules", "Tools");
            foreach (var file in new[] { "Directory.Build.props", ".github/STYLE.md", ".github/COMMITS.md" }
                .Concat(Directory.EnumerateFiles(Path.Combine(Root, "Tools"), "*.cs*").Select(path => Path.GetRelativePath(Root, path))))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(tools, file))!);
                File.Copy(Path.Combine(Root, file), Path.Combine(tools, file));
            }
            Write(module, "SDL3.Stub/SDL3.Stub.csproj", Project("<PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>"));
            Write(module, "SDL3.Stub/Stub.cs", "namespace Stub; public static class Calls { public static int One() => 1; }\n");
            Write(module, "Sample/Sample.csproj", Project(
                "<PropertyGroup><TargetFramework>net10.0</TargetFramework><GenerateDocumentationFile>true</GenerateDocumentationFile><NoWarn>CS1591</NoWarn></PropertyGroup>",
                "<ItemGroup><ProjectReference Include=\"..\\SDL3.Stub\\SDL3.Stub.csproj\" /></ItemGroup>"));
            Write(module, "Sample/Thing.cs", "namespace Sample;\n/// <summary>A thing as 3DEngine draws it.</summary>\npublic class Thing { /// <summary>One.</summary>\npublic int One() => Stub.Calls.One(); }\n/// <summary>A type no table names.</summary>\npublic class Hidden { }\n");
            Write(module, "README.md", "# Sample\n\n## Binding it in your engine\n\nMake a `Thing`.\n\n```csharp\nvar thing = new Thing();\n```\n");
            Write(module, "Sample.Tests/Sample.Tests.csproj", Project(
                "<PropertyGroup><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>",
                "<ItemGroup><ProjectReference Include=\"..\\Sample\\Sample.csproj\" /><PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"18.5.1\" />"
                + "<PackageReference Include=\"xunit\" Version=\"2.9.3\" /><PackageReference Include=\"xunit.runner.visualstudio\" Version=\"4.0.0-pre.4\" /></ItemGroup>",
                items));
            var host = "using System.Reflection;\nusing Tools;\nusing Xunit;\n\npublic class Checks\n{\n"
                + "    private const string Library = \"Sample\";\n    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, \"../../../..\"));\n"
                + "    private const string Host = \"Sample.Tests/Checks.cs\";\n\n    // The README's block from here\n"
                + string.Concat(facts.Split('\n').Select(line => line.Length > 0 ? "    " + line + "\n" : "\n"))
                + "    // The end marker.\n}\n";
            Write(module, "Sample.Tests/Checks.cs", host);

            var (code, printed) = Programs.Dotnet(Path.Combine(module, "Sample.Tests"), "test", "-nologo");

            Assert.True(code != 0, printed);
            Assert.True(printed.Contains("error CS", StringComparison.Ordinal) is false, printed);
            Assert.Contains("Remove the reference, or name it in allowed where it is a module the library may take.", printed);
            Assert.Contains("T:Sample.Thing names an engine in the library's public names or their documentation", printed);
            Assert.Contains("Say what the engine's name stood for in the module's own terms.", printed);
            Assert.Contains("Name each there in code, its name in backticks.", printed);
            Assert.Contains("so make the two the same.", printed);
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(module, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(module, true);
        }
    }

    private static string Between(string text, string fence, int from)
    {
        var open = text.IndexOf(fence, from, StringComparison.Ordinal) + fence.Length;
        return text[open..text.IndexOf("```\n", open, StringComparison.Ordinal)];
    }

    private static string Project(params string[] parts) => $"<Project Sdk=\"Microsoft.NET.Sdk\">\n{string.Join("\n", parts)}\n</Project>\n";

    private static void Write(string root, string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(root, path))!);
        File.WriteAllText(Path.Combine(root, path), text);
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
