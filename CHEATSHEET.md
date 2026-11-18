# 3DEngine.Tools cheat sheet

Every public type of the library with its public members, each a line of its declaration and the first sentence
of its summary. The list is written by `Cheatsheet.Write` from the built library, and the text around it by hand.

<!-- written by Cheatsheet.Write from the built library -->
```csharp
class ShellForms                                         // The shell forms only GNU's tools or bash 4 read, and the scripts of a checkout read for them, as N 6.6 of NORM.md asks of a script more than one system runs
IReadOnlyList<ShellForm> All { get; }                    // Every form the scripts are read for
IEnumerable<ShellForm> In(string line);                  // The forms a line holds, its comment left out
string Code(string line);                                // A line without its comment
IReadOnlyList<ShellFinding> Find(string root, IEnumerable<string> scripts);  // Every line of the scripts given that holds a form
SortedSet<string> Scripts(string root, params string[] leftOut);  // Every shell script of a checkout, each file ending in .sh, by its path from the root with forward slashes
SortedSet<string> RunElsewhere(string root, params string[] launchers);  // The scripts the jobs of a checkout's workflows run on any system but Ubuntu name, and those each of them names in turn, by their paths from the root

class DocumentCopies                                     // The sources of the documents every repository carries a copy of, STYLE.md and COMMITS.md, and the check of a copy against its source
IReadOnlyList<string> Names { get; }                     // The names of the documents the module carries the sources of
string Source(string name);                              // The source of a document by its name, as Names gives it
IReadOnlyList<string> Differences(string source, string copy, IEnumerable<KeyValuePair<string, string>>? words = null);  // Each paragraph of a source a copy lacks or holds changed or out of order, named by its first words, or none where the copy holds them all
List<string> Paragraphs(string text);                    // A text's paragraphs, each with its lines joined, a fenced block's kept as they are

record ShellForm                                         // A form of shell that GNU's tools or bash 4 read and macOS's BSD tools or bash 3.2 do not
ShellForm(string Name, Regex Pattern, string Instead);
string Name { get; init; }                               // The form as a script writes it, such as sed -i
Regex Pattern { get; init; }                             // How the form is found in a line of code
string Instead { get; init; }                            // What reads alike on both systems in its place

record ShellFinding                                      // A line of a script that holds a ShellForm
ShellFinding(string Script, int Line, ShellForm Form);
string Script { get; init; }                             // The script by its path from the checkout's root, with forward slashes
int Line { get; init; }                                  // The line's number, from 1
ShellForm Form { get; init; }                            // The form the line holds
string ToString();                                       // The finding as a test's message gives it, the place, the form and what to write instead

class ModuleChecks                                       // The checks that a module's library is one any engine takes, each returning what it finds, so a module's tests assert each empty and hold no copy of a check of their own
IReadOnlyList<string> RefusedNames { get; }              // The names no reference of a module's library may carry, the engines' own assemblies, this module, and the windows, the graphics APIs and the bindings over them a .NET program draws through
IReadOnlyList<string> Refused(IEnumerable<string> references, IEnumerable<string>? allowed = null);  // The references given that RefusedNames refuses, each once, those named in allowed apart
IReadOnlyList<string> Refused(Assembly library, string dependencyFile, IEnumerable<string>? allowed = null);  // The references of a built library that RefusedNames refuses, its assembly's own and the packages a dependency file records for it, followed to those they bring in turn, those named in allowed apart
IReadOnlyList<string> Packages(string library, string dependencyFile);  // The packages a dependency file records for a library, followed to those they bring, by their names alone
IReadOnlyList<string> EngineNames(IEnumerable<string> publicNames, XDocument documentation);  // The public names given and the documented members that name an engine, its name or its API's prefix as a word of its own
IReadOnlyList<string> EngineNames(Assembly library);     // The public types and members of a built library and the documented members that name an engine, the documentation read from the XML file beside the assembly
IReadOnlyList<string> PublicNames(Assembly library);     // A library's public types and their own public members, each by its full name
IReadOnlyList<string> Unnamed(IEnumerable<string> typeNames, string readme);  // The type names given that a README names neither in its section Binding it in your engine nor in a row of a table, each named in code whole, as `Star` or `Star.Name` and not as the start of `Stars`
IReadOnlyList<string> Unnamed(Assembly library, string readme);  // The public types of a built library that a README names nowhere a stranger reads first
string? BindingBlock(string readme, string hostSource);  // Where the first C# block of a README's section Binding it in your engine and the lines of a test's source between its two markers differ, as a sentence, or null where they are the same
string? ReferenceFault(IEnumerable<string> references, IEnumerable<string>? allowed = null);  // Null where Refused finds nothing, else a sentence naming what it found, the rule and what to do, for a test's message
string? ReferenceFault(Assembly library, string dependencyFile, IEnumerable<string>? allowed = null);  // ReferenceFault of a built library's references and its packages, as Refused reads them
string? EngineNameFault(IEnumerable<string> publicNames, XDocument documentation);  // Null where EngineNames finds nothing, else a sentence naming what it found, the rule and what to do
string? EngineNameFault(Assembly library);               // EngineNameFault of a built library
string? UnnamedFault(IEnumerable<string> typeNames, string readme);  // Null where Unnamed finds nothing, else a sentence naming the types, the rule and what to do
string? UnnamedFault(Assembly library, string readme);   // UnnamedFault of a built library
string? CheatsheetFault(Assembly library, string readme, string page);  // Null where the page at page holds what Write writes from the built library, else a sentence naming its first line that differs and what to do

class Cheatsheet                                         // Writes a module's CHEATSHEET.md from its built library and documentation file, every public type with its public members a line each, the declaration without its modifiers and the first sentence of its summary
string Marker = "<!-- written by Cheatsheet.Write from the built library -->";  // The line the page's written list stands under, followed by its C# block
string Write(Assembly library, string readme, string page);  // The page with its list written again from a built library, the documentation file read from beside the assembly, or a page of its own where the one given is empty
string Write(Assembly library, XDocument documentation, string readme, string page);  // The page with its list written again from a built library and its documentation
```
