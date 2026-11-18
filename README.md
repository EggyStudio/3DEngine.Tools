[Devlog](https://github.com/EggyStudio/Tools/blob/main/DEVLOG.md) [Cheatsheet](https://github.com/EggyStudio/Tools/blob/main/CHEATSHEET.md)

# Tools

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

Before the commit is made, `--staged` reads what the index holds by the same rules, with the message
in the file `--message` names, so a fault is mended before the commit rather than by an amend after
it, and the commit is then made from the same file.

```bash
FORBIDDEN_NAMES='<names>' python3 -I <Tools>/scripts/commitcheck.py <repo> --staged --message=<file>
git commit -F <file>
```

## A sheet of charts

`scripts/chart.py` draws a sheet of data charts on a white ground from a layout in JSON, a title and
a subtitle, panels of line charts, horizontal bars by group, tables and signed maps in a grid, and a
line of notes, and writes it in the format its name's extension gives, WebP or PNG among them. It
needs Pillow and nothing else, and names nothing of these repositories, so it stands on its own.

```bash
python3 -I scripts/chart.py <layout.json> <sheet.webp>
```

[examples/README.md](examples/README.md) shows both scripts for a reader new to them, each layout
beside the picture it draws.

The layout's `title`, `subtitle` and `notes` are lines of text, `legend` a list of `label` and
`color`, `columns` the grid's columns, `width` the sheet's width, `panel_height` a panel's height
and `font` the type's size, and `panels` a list, each with a `kind`, a `title` and a `subtitle`. A
`line` panel's `x` and `y` give an axis's `label`, `min`, `max`, `ticks` and `log`; its `series`
each have `points`, a list of x and y, a `label` drawn at the line at `label_at` on the x axis, a
`color`, `dash` for a dashed line, `marks` to mark every point and `marked` for points of its own;
its `bands` each shade from `from` to `to` on the y axis with a `label`. A `bars` panel's `groups`
each have a `label` and `values`, a bar a series and null for none, its `series` the bars' `label`
and `color`, its `x` an axis, and `value_labels` gives `last`, `all` or `none`. A `table` panel has
`columns`, `rows`, `bold`, the rows drawn heavy by their numbers from 0, and `align`, `left` or
`right` a column. A `map` panel's `values` are rows of signed numbers, red above zero and blue
below, full at `scale` and black at zero, with a `label` under it. A color is a hexadecimal one or
the palette's next. The script prints each panel's place and each chart's plot with its axes'
ranges.

```json
{
  "title": "Time a point by stage", "columns": 2, "width": 1560, "panel_height": 600,
  "legend": [{"label": "513 a side", "color": "#8db8f0"}, {"label": "4,097 a side", "color": "#0f2f66"}],
  "panels": [
    {"kind": "bars", "title": "Time a point", "x": {"label": "nanoseconds a point"},
     "series": [{"color": "#8db8f0"}, {"color": "#0f2f66"}],
     "groups": [{"label": "Noise", "values": [282.4, 143.8]}]},
    {"kind": "line", "title": "Light by height", "x": {"label": "degrees"}, "y": {"log": true},
     "series": [{"label": "the sun", "points": [[5, 4.6], [90, 118]], "dash": true}],
     "bands": [{"from": 5500, "to": 6500, "label": "daylight"}]}
  ],
  "notes": "Drawn from the bench's table."
}
```

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
| `Cheatsheet` | the writer of a module's CHEATSHEET.md from its built library |

### A module's checks

A module takes the repository as a submodule under `modules/` as an engine does and references
the library from its test project alone, so its own library and package take nothing of Tools',
and `ModuleChecks.Refused` fails a library that references it. The module adds three things, the
submodule, two items of its test project, the reference and a target that stops the build with one
sentence naming the command where the submodule is not fetched, and one test of five facts.

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

[Fact]
public void The_Readme_Opens_With_Its_Link_Row()
{
    var fault = ModuleChecks.LinkRowFault(Assembly.Load(Library), Readme);
    Assert.True(fault is null, fault);
}
```

The module's workflow checks the submodule out with `submodules: true` on `actions/checkout`. A
module allowed to take another names it by its whole assembly name, as
`ModuleChecks.ReferenceFault(library, deps, allowed: ["3DEngine.Modeling"])`, and a name that only
begins with it, or another module's, is still refused. An engine that takes a module builds the
module's library alone, which takes nothing of Tools', so it needs the module's own
`modules/Tools` no more than its tests do and clones with no recursion into it.

### A module's text

A module names no engine anywhere in it, its code, comments, README, AGENTS.md, CHEATSHEET.md, tests
and captions, its assembly's and package's name of the form `3DEngine.<Module>` apart.
`ModuleChecks.EngineNameInTextFault` reads every tracked text file of a module's checkout but its
`modules/` and its `.github/REVIEW.md` and `.github/NORM.md`, and is null where none names an
engine, else names the first line with what it names and how many more do. A module adds this fact
beside its five. `ModuleChecks.ProductNamesInText` lists each line that names another product,
Unity, HDRP, Unreal or Godot, a license's file and a line citing a source by an address or a year
apart, a warning a reviewer reads and no fact, since a module's text stands on its own terms, and
`commitcheck.py` warns of each such line a module's commit adds. This repository's scripts and
reports name the repositories they read, so its own tests run the check over made repositories
alone.

```csharp
[Fact]
public void No_Tracked_Text_Names_An_Engine()
{
    var fault = ModuleChecks.EngineNameInTextFault(Root);
    Assert.True(fault is null, fault);
}
```

### A module's cheatsheet

`Cheatsheet.Write` writes a module's CHEATSHEET.md from its built library and documentation file,
every public type in the order the README first names it, with its public members a line each, the
declaration without its modifiers and the first sentence of its summary. The list is the C# block
under the line `<!-- written by Cheatsheet.Write from the built library -->`, and the text around it
is written by hand and kept. `ModuleChecks.CheatsheetFault` is null where the page holds what the
writer writes, and names the first line that differs where it does not; run with
`CHEATSHEET_WRITE=1`, it writes the page instead, a first one where there is none. A member
documented by `<inheritdoc/>` takes the summary its `cref` names, or else the one of the member it
overrides or the interface's it implements. A positional record needs a summary on its type and a
`<param>` for each of its parameters, from which the build documents its properties and its
constructor, whose line carries no comment since its type's says it, so no constructor is written
out for the page. A module that keeps a cheatsheet adds this fact beside its five, and this
repository's own page is [CHEATSHEET.md](CHEATSHEET.md).

```csharp
[Fact]
public void The_Cheatsheet_Is_What_The_Built_Library_Writes()
{
    var fault = ModuleChecks.CheatsheetFault(Assembly.Load(Library), Readme, Path.Combine(Root, "CHEATSHEET.md"));
    Assert.True(fault is null, fault);
}
```

### What a module carries

A module pinning this repository's head carries the submodule and the test project's two items of
`bc59c55` and the facts below, each in the form the commit beside it gives, with the commits that
changed what the check reads. A module pinned before a fact's commit owes that fact.

| fact | the check it calls | in its form since | what changed it after |
|---|---|---|---|
| `The_Library_Takes_No_Engine_Window_Or_Graphics_Api` | `ModuleChecks.ReferenceFault` | `7d56e0f` | |
| `No_Public_Name_Or_Its_Documentation_Names_An_Engine` | `ModuleChecks.EngineNameFault` | `7d56e0f` | |
| `The_Readme_Names_Every_Public_Type` | `ModuleChecks.UnnamedFault` | `7d56e0f` | `183e308`, a generic type's arity |
| `The_Readmes_Binding_Block_Stands_Word_For_Word_In_The_Host` | `ModuleChecks.BindingBlock` | `7d56e0f` | |
| `The_Cheatsheet_Is_What_The_Built_Library_Writes` | `ModuleChecks.CheatsheetFault` | `17860cb` | `3720852`, a record's constructor, `183e308`, inheritdoc, `5bf6255`, infinite defaults, and `460a481`, operators, tuples and nested types |
| `No_Tracked_Text_Names_An_Engine` | `ModuleChecks.EngineNameInTextFault` | `b817e9b` | |
| `The_Readme_Opens_With_Its_Link_Row` | `ModuleChecks.LinkRowFault` | `0b0b902` | `75d77f2`, the cheatsheet as one word |

`ModuleChecks.ProductNamesInText`, from `d1b04e5`, is a list a reviewer reads and no fact.

A module's README opens with its link row, its first line above its title, the row's links to its
DEVLOG.md and CHEATSHEET.md on its repository separated by a space, then a blank line and its title,
as this README opens. `ModuleChecks.LinkRowFault` fails a README that opens otherwise.

```markdown
[Devlog](https://github.com/EggyStudio/Tools/blob/main/DEVLOG.md) [Cheatsheet](https://github.com/EggyStudio/Tools/blob/main/CHEATSHEET.md)

# Tools
```

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
python3 -I scripts/devlog.py <repo path> <Name> [sent-pictures.json] [--fixes <fixes.json>] [--commits]
```

With `--commits` each day lists the commits of that day by their sentences, newest first, a
message's first line under its subject or its two where it has two, a commit that changes the
review's files or DEVLOG.md alone left out, so a repository whose review records no item done reads
what landed in its devlog. A commit whose sentence names an engine outside the form of a module's
assembly, `3DEngine.<Module>`, is left out of the page and counted on the script's output alone,
since pushed history is not rewritten, and the commit's check fails such a message in a module. A
commit whose line on the page would fail the check an added line of a page gets, the cheatsheet in
two words or a middle dot among the rest, is left out and counted the same way, and for this
leave-out alone the bridge, which stood for an engine in the modules' old sentences, is read as
naming one.

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
python3 -I modules/Tools/scripts/devlog.py . Levels --commits
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
gives. A layout's `title` and `subtitle` stand over the whole sheet, then a row's title stands first
on a line of its own in a larger type, and the panels' labels under it, the first row's taking the
columns' headings where a panel has none. It needs Pillow.

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
apart, and added prose holds no em or en dash, no middle dot and no British spelling. The cheatsheet
written as two words fails on any added line of any repository, and the sweep reads every line for
it. In a repository under `Modules` other than this one, an added line of a text file that names an
engine outside the form `3DEngine.<Module>` fails, the module's own `modules/` and its review's and
norm's files apart. A picture added under `.github/assets/readings` stands on a white ground and is
saved smaller under `--thumbs` to be looked at, which needs Pillow.

```bash
python3 -I scripts/commitcheck.py <repo> [<range or commit> ...] [--thumbs=<folder>]
python3 -I scripts/commitcheck.py <repo> --staged [--message=<file>] [--thumbs=<folder>]
```

With `--staged` the index is read as a commit not yet made, printed as `staged`: its files against
HEAD, its pictures as the index holds them, the identity git would give the commit, and the message
in the file `--message` names, which where none is given is not read, a note saying so. Nothing
staged is said and exits 0, and a commit given with `--staged` is refused with 2.

A FAIL line stops a push, a WARN line is for the reviewer to read, and a note says what to look at.
The exit code is 0 where no line is a FAIL, 1 where a line is, and 2 where the check could not run,
with the reason written to the error output. The names no commit may carry are a regular expression
in the `FORBIDDEN_NAMES` environment variable and in no file, and where it is not set the check says
so.

## A commit's report

`scripts/report.py` prints a commit's report block, every line of it read by the script: the
commit's hash and sentence, its files by kind and its lines added and removed, the facts and the
theories' rows of data its diff adds to C# files, the counts of a test run read from a TRX file, the
one `--trx` names or the newest under the repository's TestResults folders, and `commitcheck.py`'s
exit code with its FAIL and WARN lines counted. A count it could not read is said to be unread, with
why, and a commit git cannot read gives exit code 2.

```bash
dotnet test Tools.Tests --logger trx
python3 -I scripts/report.py . [<commit>] [--trx=<results.trx>]
```

## A module's pin

`scripts/pin.py` moves a module's pin of Tools to a head, run from the module's root by its own
session: it fetches `modules/Tools` and checks out the head, `origin/main` where none is given,
prints the commits the pin moves by, and names from the table of what a module carries at the old
pin and the new each fact new to the module and each whose row changed, with the commits that
changed it. Where the module keeps a CHEATSHEET.md it runs its test project's cheatsheet fact with
`CHEATSHEET_WRITE=1`, which writes the page again, `--tests` naming the project where none ending in
`.Tests` holds it, and it writes DEVLOG.md again with the new pin's devlog.py and `--commits`. It
commits nothing, so the session reads the change and commits it. A folder outside git or a
repository with no `modules/Tools` is refused, and the exit code is 0, or 2 where git, the fetch or
dotnet could not be reached.

```bash
python3 -I modules/Tools/scripts/pin.py [<head>] [--tests=<test project>]
```

## The repositories' status

`scripts/status.py` prints a line a repository for the hourly watch and writes nothing: its local
and remote heads, read by `git ls-remote`, which fetches nothing, and how many commits it is ahead,
its last build run's head, status and conclusion through gh, the size of its `.github/REVIEW.md`
where it passes 90 KB, whether its DEVLOG.md was last committed before its newest picture, its pin
of Tools against Tools' head, and whether its README opens with the link row. With no repository
given it reads 3DEngine, BevyCSharp and Voxel-Sandbox beside the Modules folder this checkout stands
in, and every folder of Modules, the eleven in 2.5 seconds. The exit code is 0, or 2 where git or gh
could not be reached for a repository, its line saying which.

```bash
python3 -I scripts/status.py [--tools=<Tools checkout>] [<repo> ...]
```

## A commit's hook

`scripts/hooks.py` installs in the repository the working folder lies in a post-commit hook that
runs `commitcheck.py` on the commit just made and prints its lines, so a session sees a FAIL as it
commits and amends before it reports. The hook stops no commit and exits with 0 whatever the check
finds; where Python or the check's script is missing it prints nothing. The names the check reads
come from `FORBIDDEN_NAMES` in the committing process's environment, which the hook neither reads
nor writes. A repository the working folder does not lie in is refused, so a session installs the
hook in its own repository alone, and a post-commit hook that is not this script's is left as it is.
`remove` takes the hook out.

```bash
python3 -I modules/Tools/scripts/hooks.py install
python3 -I modules/Tools/scripts/hooks.py remove
```

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
build/variants.sh
```

`build/variants.sh` runs the tests again on Linux as Windows and macOS meet a script, the temporary
folder reached through a symbolic link as macOS reaches its own, every program given a single-byte
locale as Windows gives a pipe, and git turning line ends to CRLF as Git for Windows does by its own
setting, so a fault of either system fails on Linux first. It needs the locale en_US.ISO-8859-1,
which `sudo locale-gen en_US` installs on Ubuntu, and says so where it is missing. A script that
ends in a traceback fails its test whatever exit code the test expects.

## License

The Mozilla Public License 2.0, in [LICENSE](LICENSE).
