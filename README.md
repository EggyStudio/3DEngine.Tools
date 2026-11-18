# Tools

[DEVLOG.md](DEVLOG.md)

Tools is a library and a set of scripts that any engine takes as a git submodule, for what its
repositories otherwise copy by hand, the devlog's generator, the composer of reading sheets, the run
reader, the check of a commit and the sweep of a tree, the reading of shell scripts for forms macOS
does not read, and the sources of STYLE.md and COMMITS.md with the check of a copy against them. It
reads files alone, so it references no engine and declares no interface, and an engine of any kind
takes it as it is.

## For a session

A session runs the commit's check on its commits before it reports them, from this checkout, with
the names it is given in the environment, every commit not yet pushed being read where no range is
given.

```bash
FORBIDDEN_NAMES='<names>' python3 -I <Tools>/scripts/commitcheck.py <repo> [origin/main..HEAD]
```

A FAIL line is a commit that breaks the form or a rule a pattern reads, which is mended and the
commit made again before it is reported. A WARN line is for the reviewer to read and decide, and the
report names each one left standing. A note says what to look at, such as a picture's smaller copy,
and a WARN that FORBIDDEN_NAMES is not set says the names were not read for. The exit code is 0
where no line is a FAIL, whatever WARN lines stand, 1 where a line is, and 2 where the check could
not run, a revision git does not know or a pattern that is no regular expression among the causes,
which it names, so `commitcheck.py <repo> && git push` pushes only a commit with no FAIL.

## Binding it in your engine

The module needs nothing of an engine. It reads a checkout's files and the pictures a program has
written, so it declares no interface for an engine to implement and an engine of any kind takes it
as it is. An engine adds the repository as a git submodule under `modules/` and references the
library from its test project alone, since nothing a program ships calls it, here from a project one
folder below the engine's root.

```bash
git submodule add https://github.com/EggyStudio/Tools.git modules/Tools
```

```xml
<ProjectReference Include="..\modules\Tools\Tools\Tools.csproj" />
```

The library is built on .NET 10 with no package of its own as the assembly `3DEngine.Tools`, its
types in the namespace `Tools`, and its `Directory.Build.props` stops it taking an engine's
`Directory.Build.targets` from the folders above. The engine then adds one test
that calls `ShellForms` over its scripts and one that calls `DocumentCopies` over its STYLE.md and
COMMITS.md with its own word for the module's, as the sections below show, and runs the scripts by
their path under `modules/Tools/scripts` with Python 3, Pillow where a picture is read or written.

| type | what it is |
|---|---|
| `ShellForms` | the shell forms only GNU's tools or bash 4 read, and a checkout's scripts read for them |
| `ShellForm` | one such form, with what reads alike on both systems in its place |
| `ShellFinding` | a line of a script that holds a form |
| `DocumentCopies` | the sources of STYLE.md and COMMITS.md, and a copy read against its source |
| `ModuleChecks` | the checks that a module's library is one any engine takes |

### A module's checks

A module takes the repository as a submodule under `modules/` as an engine does and references
the library from its test project alone, so its own library and package take nothing of Tools',
and `ModuleChecks.Refused` fails a library that references it. The module adds three things, the
submodule, two items of its test project, the reference and a target that stops the build with one
sentence naming the command where the submodule is not fetched, and one test of four facts.

```bash
git submodule add https://github.com/EggyStudio/Tools.git modules/Tools
```

```xml
<ItemGroup>
    <ProjectReference Include="..\modules\Tools\Tools\Tools.csproj" />
</ItemGroup>
<Target Name="NeedsTools" BeforeTargets="Restore;CollectPackageReferences;ResolveProjectReferences" Condition="!Exists('$(MSBuildThisFileDirectory)..\modules\Tools\Tools\Tools.csproj')">
    <Error Text="The Tools submodule is not fetched. Run git submodule update --init modules/Tools." />
</Target>
```

The test's facts read the module's built library by its assembly's name, `Library`, its checkout's
root, `Root`, and the source of the test whose host runs the README's own binding block, `Host`,
the three a module sets above the block. That host holds the module's binding block between a line
holding `// The README's block from here` and a line holding `// The end marker.`, which the fourth
fact reads against this README's section word for word. Each fact checks one thing, so a module
sees every fault at once, and a fault's message names the rule and what to do, given to
`Assert.True` whole where `Assert.Null` would print its first fifty characters.

```csharp
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
```

The module's workflow checks the submodule out with `submodules: true` on `actions/checkout`. A
module allowed to take another names it by its whole assembly name, as
`ModuleChecks.ReferenceFault(library, deps, allowed: ["3DEngine.Modeling"])`, and a name that only
begins with it, or another module's, is still refused. An engine that takes a module builds the
module's library alone, which takes nothing of Tools', so it needs the module's own
`modules/Tools` no more than its tests do and clones with no recursion into it.

A clone of the engine fetches the module with `git clone --recurse-submodules`, or with
`git submodule update --init` in a clone made without it, and a workflow checks it out with
`submodules: true` on `actions/checkout`. A change the engine uses is committed and pushed here
before the engine's commit that moves its pointer, so the engine's history never names a commit
this repository does not have.

## The devlog

`scripts/devlog.py` writes a repository's `DEVLOG.md`, what landed by day and newest first. It
takes the pictures under `.github/devlog` or `.github/assets/readings` with their captions and the
notes beside them, the captures under `.github/assets`, and the items of `.github/REVIEW.md` and
`.github/REVIEW-ARCHIVE.md` marked done, each on the day of the commit that closed it. A repository
runs it with its path and its name, and with a list of the pictures sent on the way where it keeps
one, which needs Pillow to save them as WebP.

```bash
python3 -I scripts/devlog.py <repo path> <Name> [sent-pictures.json] [--fixes <fixes.json>]
```

`--fixes` names a JSON file kept outside the repositories, an object whose `pairs` are a pattern and
its replacement each, applied in turn to every caption and title, whose `patterns` give by a
repository's name the pattern its STYLE.md adds to the check, and whose `undated` is the day a sent
picture with no date is put on. With none given it rewrites nothing, adds no pattern and stops at a
sent picture with no date, naming it.

It prints the days, pictures and items it wrote, and any line that breaks STYLE.md's mechanical
rules, and with no repository given it prints how it is run.

A module keeps its own devlog, generated again in every commit that closes an item of its review, by
the script of the Tools it takes as its submodule, run from the module's root with its name.

```bash
python3 -I modules/Tools/scripts/devlog.py . Levels
```

## A workflow run

`scripts/run.py` reads a GitHub Actions run through `gh api`, so the login `gh` holds serves. It
prints the run's jobs with their conclusions and times and, for each job that failed, timed out or
was canceled, the step it stopped at and the annotations of its check run. The last 60 lines of
that step go to a file in the folder given, whose path it prints, and never to the terminal.

```bash
python3 -I scripts/run.py <account/repo> <run id> <folder for the logs>
```

## A sheet of pictures

`scripts/sheet.py` composes a sheet on a white ground from a layout in JSON, rows of panels with a
heading a column and a title a row in dark text, and writes it in the format its name's extension
gives. A row's title stands first on a line of its own in a larger type, and the panels' labels
under it, the first row's taking the columns' headings where a panel has none. It needs Pillow.

```bash
python3 -I scripts/sheet.py <layout.json> <sheet.png>
```

```json
{
  "font": 16,
  "size": [320, 180],
  "columns": ["frame", "path traced", "frame less it"],
  "rows": [
    {
      "title": "The room at noon",
      "gain": {"mean": "noon/ref.pfm", "to": 0.18},
      "panels": [
        {"image": "noon/frame.pfm", "boxes": [[160, 440, 480, 680]]},
        {"image": "noon/ref.pfm", "label": "4,096 paths a pixel"},
        {"signed": ["noon/frame.pfm", "noon/ref.pfm"], "share": 0.5}
      ]
    }
  ]
}
```

A panel is a capture under `image`, a picture Pillow opens or a PFM of linear light, shown in sRGB
at its gain. Under `signed` it is the first capture less the second in luminance, red where brighter
and blue where darker, full at `share` of the second's mean, on black, the one dark panel of a
sheet. Under `difference` it is the largest
channel's difference of two pictures as shown, times `times`, 4 where none is given, dark on white.
`gain` is a number or the gain that puts a capture's mean luminance at `to`, 1 where none is given.
`crop` is a box in the capture's pixels, left, top, right and bottom, `size` the panel's size in
pixels, the cropped capture's where none is given, and `label` a line over one panel. `gain`,
`crop`, `size` and `share` are given on the layout, a row or a panel, the nearest applying. Paths
are read from the layout's folder.

A box under `boxes` is given in the capture's own pixels and drawn on a capture's panel only, with
the mean luminance in linear light it reads there written at its corner. The script prints the
place of each title, each row's labels and each panel on the sheet, and each box's reading.

## A commit's check

`scripts/commitcheck.py` reads commits against the form COMMITS.md gives and the rules of NORM.md a
pattern can read, so a session runs it on its own commit before reporting it. The message is the
three marks, a blank line and one sentence ending in a full stop, with no review's number, no tool's
name, no colon inside it and no spaced hyphen; author and committer are the repository's identity; a
merge fails. An added comment line runs to 100 columns at most, a line holding an address alone
apart, and added prose holds no em or en dash, no middle dot and no British spelling. A picture
added under `.github/assets/readings` stands on a white ground and is saved smaller under `--thumbs`
to be looked at, which needs Pillow.

```bash
python3 -I scripts/commitcheck.py <repo> [<range or commit> ...] [--thumbs=<folder>]
```

A FAIL line stops a push, a WARN line is for the reviewer to read, and a note says what to look at.
The exit code is 0 where no line is a FAIL, 1 where a line is, and 2 where the check could not run,
with the reason written to the error output. The names no commit may carry are a regular expression
in the `FORBIDDEN_NAMES` environment variable and in no file, and where it is not set the check says
so.

## A tree's sweep

`scripts/sweep.py` reads a checkout's tracked files as they stand by the rules `commitcheck.py`
reads a commit's added lines with, a comment line's width, the comment patterns, the middle dot, the
dashes, the spelling and the names `FORBIDDEN_NAMES` gives, and every picture under
`.github/assets/readings` or `.github/devlog` by the pictures' rule. It writes nothing. It prints a
count a rule with its first ten places, any name in a place put out of sight, then the files read
and the time taken, and exits with 1 where a rule that fails a commit is broken. It read 3DEngine's
3,064 tracked files, 1,948 of them text and 134 pictures, in 2.5 seconds (python3 -I
scripts/sweep.py ../../3DEngine).

```bash
python3 -I scripts/sweep.py <repo>
```

## A day's sweep

`scripts/daily.py` sweeps the repositories given by `sweep.py`'s rules and writes one Markdown
report to the path given, a row a repository with its counts of failures and warnings, then for each
its count a rule, a table by rule and top folder, largest first, with how many of each the rewrap
would clear beside how many are left for hands, read by `rewrap.py` without writing, a table of the
hand work, what a hand does with each failure the rewrap leaves, with its count and three places,
its counts a top folder and the ten files a rule holds most of. Given the report before, it reads
that report's counts from the JSON at its end and gives the change in each count, a rule no longer
broken among them. It reads the repositories and writes the report alone, and any name
`FORBIDDEN_NAMES` gives is put out of sight in a place and a path. It read 3DEngine, BevyCSharp,
Voxel-Sandbox and this repository in 17.5 seconds at a load of 13 (python3 -I scripts/daily.py
<report> ..., uptime).

```bash
python3 -I scripts/daily.py <report.md> [--previous=<report.md>] <repo> ...
```

## A comment's width

`scripts/rewrap.py` rewraps the comment lines past 100 columns of the files a repository tracks
under each path given, as `git ls-files` lists them, with their words unchanged, filling again each
paragraph that holds one, in `//` and `///` comments of the C family, `#` comments of Python, shell,
YAML and TOML and `<!-- -->` comments of MSBuild and XML. An ignored file, an untracked one and a
submodule's are never read or written, and the files are those `sweep.py` reads, so the lines
rewrapped and those left as they stand add up to the sweep's count of comment lines past the width.
A blank comment line, a documentation tag alone, a list's item and a line indented further keep
their lines, a tag stays whole, a comment after code is left as it is, and a word longer than the
width stands on a line of its own. Without `--write` it changes nothing and prints each file it
would change and the counts. A file whose lines of code would differ after the rewrapping, or whose
comments' words would differ once their blanks are joined, is refused, left as it is and makes the
exit code 1.

```bash
python3 -I scripts/rewrap.py [--write] <file or folder> ...
```

## A rewrap's proof

`scripts/rewrapcheck.py` proves the rewrap safe to run over the repositories given by running it
over a copy of each alone, the tracked files of the languages the rewrap reads copied into a folder
of their own under the work folder given and made a repository there. A file holds where its code,
every line but its comment lines as this script reads them apart from the rewrap's rule, is
byte-equal before and after, its comments' words are the same once their blanks are joined, and a
second run changes nothing. The repositories are read and never written, and a work folder inside
one is refused.

```bash
python3 -I scripts/rewrapcheck.py <work folder> <repo> ...
```

## The shell's forms

`ShellForms` in the library reads a checkout's shell scripts for the forms only GNU's tools or bash
4 read, such as `sed -i` with no suffix, which macOS's BSD `sed` reads as a script name, each with
what reads alike on both systems in its place. A repository's test calls it with its root, over
every script of the checkout or over those its workflows' Windows and macOS jobs run and the scripts
they name in turn, and asserts it finds nothing.

```csharp
var found = ShellForms.Find(root, ShellForms.Scripts(root, "build/tools"));
Assert.True(found.Count == 0, string.Join("\n", found));
```

## The documents' copies

`DocumentCopies` in the library carries the sources of STYLE.md and COMMITS.md, the files under
`.github` here, and reads a repository's copy against its source. A copy holds every paragraph of
the source in the source's order, with the repository's words put in for the module's, and adds
paragraphs of its own between them; a paragraph is read with its lines joined, so wrapping the lines
again changes nothing. Each paragraph the copy lacks, changes or holds out of order is named by its
first words.

```csharp
var copy = File.ReadAllText(Path.Combine(root, ".github", "STYLE.md"));
var differences = DocumentCopies.Differences(DocumentCopies.Source("STYLE.md"), copy, [new("module", "engine")]);
Assert.True(differences.Count == 0, string.Join("\n", differences));
```

## Building and testing

With the .NET 10 SDK:

```bash
dotnet build Tools.slnx
dotnet test Tools.Tests
```

## License

The Mozilla Public License 2.0, in [LICENSE](LICENSE).
