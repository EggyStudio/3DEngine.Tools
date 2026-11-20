# Review

Reviewed up to `13c8202`, item 2, pushed at 12:31 on 2026-10-31 with `e28b3b4`, item 1, after the
owner's first commit, its run green at 12:32, the first of this repository. After it `7532128`, item
3's first part, the devlog generator, pushed at 12:35, its run green at 12:35 as well;
scripts/devlog.py as the reviewing session's byte for byte, the README saying how a repository runs
it, DevlogTests making a one-commit scratch repository with git's identity given on the command line
and reading the Done line the script writes, Python found as python3 or python and the test skipped
with its reason where neither runs; 2 of 2 tests, the strict build clean. After it `321de49`, item
3's second part, the run reader, pushed at 12:38, its run green at 12:41; scripts/run.py prints a
run's jobs with their conclusions and times and, for a job that failed, timed out or was cancelled,
the step it stopped at and the check run's annotations, writing the step's last 60 lines to a file
under the folder given and printing the path, no log text reaching the terminal; the step is bounded
by its own group header and the next, since steps carry whole-second times and the first version
took the next step's header in; gh's escape flag added only where gh asks; the test reads recorded
answers keyed by API path, so it skips only where Python is missing; 3 of 3 tests, the strict build
clean. Both commits keep the form, the three-mark subject, one sentence, the owner's identity as
author and committer, no trailer (N 7.2 and N 7.6 kept at the first commit); the library references
no engine (N 2.1). The documents are the engines' with the engine's name replaced and the
engine-only lines dropped, COMMITS.md the game's with N 7.1's pointer rule in place of the legacy
branch line, .gitignore with the dump folder, .claude and .ref; AGENTS.md's devlog row carries no
link until DEVLOG.md exists and its table of areas gains the rows item 2 adds. The skeleton sets
ImportDirectoryBuildTargets false, since a submodule under modules/ would inherit an engine's
Directory.Build.targets, makes a missing summary an error, loads the assembly by name in its one
test, and build/version.sh prints 0.1.0; the strict build 0 warnings and 0 errors, the test passing.
After it `1c0015a`, the norm alone, the seven repositories named, pushed at 14:51 on 2026-11-03,
with no run since the build passes over the review's files; it keeps the form. Before it `2340033`,
the owner's first commit alone.

## Now

The order the owner set on 2026-10-31 for the six modules, this one holding the scripts and the
documents' sources the repositories duplicate by hand, the devlog generator, the sheet composer of
3DEngine's item 136, the run reader, the texture recipes of `build/textures.py`, and STYLE.md,
COMMITS.md and the common rules of NORM.md. The documents first, then the rest as the other projects
have it, then the engines taking the module. Items 1 and 2 are the game's session's, being free
while the game waits on the engine's package; item 3 is the session's that owns the code today; item
4 is each engine's session's. Among the six this module comes first, since it needs no engine code
and every repository gains from it at once.

1. **The documents, one commit.** AGENTS.md on the game's shape (one paragraph saying what the
    module is and that no engine is referenced, N 2.1; the documents table with STYLE, COMMITS,
    REVIEW, NORM and DEVLOG; building and testing; Where things are with a row a folder; the
    conventions, the commit form among them), README.md (what the module is, how an engine takes it
    as a submodule, `dotnet build` and `dotnet test`), `.github/STYLE.md`, `COMMITS.md` and
    `CODE_OF_CONDUCT.md` as the engines' with the engine's name replaced, `.gitignore` of the
    engines' shape with `/.github/dump/` in it, `.gitattributes`. The game's AGENTS.md and the
    engines' documents are the models; nothing is written that the module does not have yet. This
    commit is the first test of the commit form here, N 7.2 and N 7.6, read by the reviewing session
    before it is pushed. Done at `e28b3b4`.

2. **The skeleton, one commit after item 1.** `Tools.slnx`, `Tools/Tools.csproj` as a class library
    on net10.0 with `Nullable` and `ImplicitUsings` in `Directory.Build.props`, `Tools.Tests/` on
    xunit with one test that the library loads, `build/version.txt` at `0.1` and `build/version.sh`
    as the engines', and `.github/workflows/build.yml` on a push, `test (os)` on ubuntu-24.04,
    windows-latest and macos-15 with `dotnet build Tools.slnx -warnaserror` and `dotnet test
    Tools.Tests --no-build`, a time limit on every job with its reason, `permissions` read and the
    engines' concurrency group. Green on the three systems before item 3. Done at `13c8202`.

3. **The code's move.** The source is the reviewing session's devlog.py and run reader, the engine
    sessions' scripts, and the documents as they stand in the engines; each repository then carries
    a copy its build checks against the source. One commit a part, the tests coming with the code,
    the strict build and the suite green, a Shared line in the engines' REVIEW.md when a part lands.
    Items written by the reviewing session when item 2 is green and the owning session is free.
    First part done at `7532128`, the devlog generator with its test; Second part done at `321de49`,
    the run reader; the sheet composer is the part left, from 3DEngine's session.

4. **The engines take the module.** Each engine adds this repository as a git submodule under
`modules/` and references its project, its workflows checking submodules out, its own folder for
this code becoming the binding of the module's interfaces (N 2.1) and the rest deleted; a change
used by an engine is committed here first and pushed before the engine's commit that moves the
pointer. One commit an engine, after item 3's first part.

## Verdicts

Two red runs so far, the first mended in the commit after it.

1. **Run 44345093619 of d3add46 was red on Windows alone (2026-11-18).** The sweep's tests split the
    script's output on a line feed and kept the carriage return, and Python wrote the pipe in the
    system's code page, which cannot hold every character the sweep prints. 7dbd4b3 mends both,
    every script setting its output to UTF-8 and one helper splitting lines of either ending, and
    run 44346168120 is green on the three systems. A test that reads another program's output is
    read on Windows before it is pushed.

2. **Run 44352106432 of 9017360 was red on macOS alone (2026-11-18).** Four of RewrapTests failed
    there, the script rewrapping nothing in a made repository under the system's temporary folder,
    where it rewrote every file on Linux and Windows. The folder is reached through a symbolic link
    on macOS, so a path read from git and the path given can differ; the cause is to be shown by the
    mend's failing test. 6b711ed waits for that mend.

## Decisions

1. **The repository's birth.** The owner made this repository on 2026-10-31 at 12:08 among six,
Modeling, Vegetation, Levels, Landscape, RadianceCascades beside it, after the ideas recorded as
3DEngine's Decision 78 and BevyCSharp's Decision 71, to hold the scripts and the documents' sources
the repositories duplicate by hand, the devlog generator, the sheet composer of 3DEngine's item 136,
the run reader, the texture recipes of `build/textures.py`, and STYLE.md, COMMITS.md and the common
rules of NORM.md as one library both engines use rather than one implementation an engine. The owner
set the order the same day, the documents first, then the rest as the other projects have it, then
the engines taking the module, and said the modules may be tried at once beside the engine's global
illumination work (3DEngine Decision 79).

2. **A git submodule, not a package.** The owner chose on 2026-10-31 that the engines take the
module as a git submodule built as a project of their own, since a package's upload, wait, test and
pack is slower to iterate than a commit. A change is committed here first; the reviewing session
pushes this repository before the engine's commit that moves its pointer; the workflows check
submodules out. The game keeps using the engine through the engine's package.

3. **The module knows no engine.** N 2.1. The module references no engine, package or bridge; what
it needs of a renderer, a world, a scheduler or a file system it takes through interfaces it
declares, which each engine implements where it binds the module. A type of an engine's in this
repository is a fault.

4. **The repository's terms are the owner's.** The repository was made by the owner with its terms
   file in place, which stands as the owner set it and is spoken of nowhere else.

5. **Who writes here.** The game's session writes items 1 and 2 for the six modules, being free
while the game waits on the engine's package; the session that owns the code today moves it (item
3), BevyCSharp's for Vegetation and Levels, 3DEngine's for Modeling and the cascades, the reviewing
session's scripts going into Tools; each engine's session binds the module (item 4). Every session
reads REVIEW.md's head, its items and the Replies before a batch, as N 7.7 says, and the reviewing
session pushes.

6. **The module is worked by a session of its own.** Decided at 16:43 on 2026-11-18 for the seven
    modules at once. From its first commit the module's session alone writes the module's source,
    tests and documents, and the engines' sessions write here no longer; an engine's coder that
    needs something of the module asks the module's session in a message and takes the commit by
    moving its submodule's pointer. Every open item above that names an engine's or the game's
    session as its worker is the module's session's, apart from the binding inside an engine, which
    stays that engine's. The session's first Reply reads this file against the tree and says what is
    done, what is open and the order it proposes; the review answers, and that order stands. The
    rules are NORM.md's as before, among them no engine referenced (N 2.1), tests that need no
    window and no GPU, the commit's form, a failing test before a mend, a picture on a white ground
    where a thing is seen, and any GPU program through the lock of N 7.9 with a waiting file. The
    session reports each checked commit to the reviewing session, which reads it, pushes it and
    reads its run; it does not push.

7. **The module is agnostic enough that a stranger binds it in an engine of their own from its
    README alone (2026-11-18 17:28).** It asks a README section, Binding it in your engine, with
    every interface the module declares, the conventions it stands on and the smallest binding that
    works, a small host in the tests that implements those interfaces with no engine and that the
    section's steps are run against, a test that fails where the library references an engine, a
    window or a graphics API, the form in the Tools module's ReferenceTests.cs, and no engine's name
    in a public member or its summary. The two engines appear in the README as examples. A public
    shape that fits the two engines alone is named in a Reply before it changes.

8. **The package id and the assembly are 3DEngine.Tools and the namespace stays Tools (2026-11-18
    17:28).** The prefix gives a unique name. A package is published when the time is right and on
    the owner's word, for a reader who takes the module alone. The engines keep taking the module as
    a git submodule, compiled with them and carried inside each engine's one package. Folders and
    project file names stay.

## Replies

The sheet of charts in the commit carrying this line, as the owner asked how others could make the
sheets. `scripts/chart.py` draws from a layout in JSON a white sheet with a title, a subtitle, a
legend and a line of notes, and panels in a grid: line charts on linear or logarithmic axes, their
series solid or dashed, labeled at the line, their points marked and a band shaded with its label;
horizontal bars by group, a bar a series, labeled at the last bar to three figures; a table with a
heavy header and bold rows; and a signed map, red above zero and blue below and black at zero as
sheet.py's signed difference is. It prints each panel's place and each chart's plot with its axes'
ranges, writes the format its name gives, needs Pillow alone and names nothing of these
repositories. The proof is Landscape's `2026-11-18-item-3-bench.webp` drawn again from the table of
its note `2026-11-18-item-3-bench.md` alone, the tiles' index array a tile from its prose, the same
stages, sizes, colors and values as the sheet its session's own script drew; the first drawing
stopped the axis at 400 short of the longest bar and labeled to four figures, which the second
mends. It is the devlog's picture `2026-11-18-chart-bench.webp`. ChartTests composes a made layout
of every kind and reads its pixels at the printed places: the white ground, the title and the notes,
a solid line's color at its value and a marked point's beside it, a band's shade, a dashed line both
inked and broken, a logarithmic axis's middle at 100 of 1 to 10,000, a bar's color inside its
printed box and white past its end, a bold row darker than the same text plain, and a map's red,
blue and black; and a panel of another kind refused by name. The script was written before its
tests, which passed at their first run, so they read it rather than failing first. The README gives
its keys with an example, and AGENTS.md's scripts row names it, a new path. The strict build is
clean and the tests pass, 151 and one skipped plain and 152 in the variant.

`d5c09a5`'s red run on Windows mended in the commit carrying this line. The cause is the line ends.
Git for Windows turns them to CRLF by its own setting, `core.autocrlf=true`, and StatusTests'
fixture cloned the made Tools repository with the machine's setting where every other git call it
made held `core.autocrlf=false`, so the clone wrote `t1.txt` with CRLF, and the checkout of the pin,
run with the setting off, read the file as changed and refused. The variant takes the class:
`build/variants.sh` gives git a global setting of `core.autocrlf=true`, and under it the test failed
first on Linux with Windows' own message, the one test of the suite to fail, as on Windows. The
fixture clones and commits through the one call that holds the setting off, and both runs pass.
VariantTests reads git's setting as well, and the README names the third condition. The strict build
is clean and the tests pass, 151 and one skipped plain and 152 in the variant.

The second proof of the charts and the examples' page in the commit carrying this line. `491595f`'s
run (44370541227) is green on the three systems. Sky's `2026-11-18-daylight-by-height.webp` is drawn
again from the table of its Reply alone, the sun's, the IES clear sky's and all the light's lux and
the sun's, the sky's and all the light's kelvin at six heights from 5 to 90 degrees, as
`2026-11-18-chart-daylight.webp`, each series on its panel with daylight's 5,500 to 6,500 K shaded.
The layout lacked three things the module's sheet has: every degree, where the table holds six
heights, so the six are marked and joined; the series of all the light with the molecules at the
primaries' wavelengths, which the table does not hold, so it is left out; and labels set clear of
the other lines. Two faults of the writer that drawing found are mended, a test failing first each:
a label stands above or below its line by `label_side` and clear of its own line across its width,
where a label over a rising line crossed it, 97 of its pixels under the line; and an axis given its
range draws no round tick past it, where 100 stood over the panel's title on an axis of 20 to 90.
sheet.py reads a picture's bands by `tobytes` where `getdata` warned that Pillow 14 removes it.
`examples` holds a page for a reader new to both scripts, two line charts, bars by group, a table
beside a signed map, and captures with their difference and their boxes' readings, each with its
picture, the command that draws it and its layout, of made-up values and naming nothing of these
repositories, with the two pictures the captures' sheet composes. ExamplesTests draws every layout
of the folder and holds the page to each layout's text word for word beside its picture, so the page
and the files cannot part. The README links the page and AGENTS.md's table names the new folder (N
1.5). The strict build is clean and the tests pass, 156 and one skipped plain and 157 in the
variant.

Two findings of the modules in the commit carrying this line, each with a test that failed first.
The cheatsheet wrote an infinite or undefined default by the number's own text, `Infinityf` and
`-Infinityf`, which is no C#, as Landscape's Raycast reach and two rules' ranges have it; it writes
`float.PositiveInfinity`, `float.NegativeInfinity` and `float.NaN`, and the same of a double,
CheatsheetTests reading a sample whose three defaults are of the three. And sheet.py drew rows'
titles with no title over the whole sheet, which N 4.9 asks first; a layout takes `title` and a
`subtitle` drawn above the first row and printed with their places, SheetTests reading the title
above the subtitle above the row's title and the title's ink. The examples' captures sheet takes a
title and a subtitle, its page's layout with it. The README says it. The strict build is clean and
the tests pass, 158 and one skipped plain and 159 in the variant.

The tests' scripts in the commit carrying this line, the first of the three by the review's order. A
test ran a script from the copy the build left beside the tests, which stayed as it was until the
next build, so a test run with no build after an edit read the old script and could pass what the
edit should fail, as two of this day's runs did. `Programs.ScriptPath` names a script in the
checkout's own scripts folder, every script a test runs or copies is read from there, and the test
project copies none beside the tests. ProgramsTests failed first, reading the script's path as the
checkout's and no scripts folder beside the tests. A message changed in commitcheck.py failed its
test with no build between and passed again when it was put back. The strict build is clean and the
tests pass, 159 and one skipped plain and 160 in the variant.

The cheatsheet's catalogue of forms in the commit carrying this line, the second of the three. The
writer was read first over the seven modules' built libraries and documentation, read and written
nothing to, for lines no C# writes, and three forms in their surfaces read wrong: an operator by its
method's name, Levels' `LevelPoint operator Addition(...)`; a tuple inside a generic argument as
`ValueTuple<...>`, since a tuple of a generic's parameters has no full name, RadianceCascades'
`IReadOnlyList<ValueTuple<..., bool>>`; and a nested type of a generic one given its outer type's
parameters as its own, `SceneFieldPlan<TMesh, TTag>.Instance<TMesh, TTag>`. A sample type of every
form a module's library takes or may stands in the tests' project, its documentation the
compiler's, and CheatsheetTests holds each form's line as C# writes it, 20 of them: the arithmetic,
equality, implicit and explicit operators, an override, a generic class, a tuple of a nested type of
a generic one, an `in` and an `out` parameter of a generic method, a flags enum and its member, a
constant and a static field, an event, an indexer, a `ref` and a `params` parameter, a nullable
value with a default and a combined flags default, and an extension method. Nine failed first: the
six operators, the tuple, the nested type, and the combined flags default, which was written as
`Ways.` with no member. The writer writes an operator by its symbol and a conversion by its kind,
reads a tuple by its generic definition, gives each level of a nested type its own count of the
arguments, and writes a flags value as its members joined by a bar or as a cast where no member
names it. Over the modules at their heads Levels' operators read `LevelPoint operator +(...)` and
RadianceCascades' tuples `IReadOnlyList<(SceneFieldPlan<TMesh, TTag>.Instance, bool)>`. A tuple's
element names, which an attribute of the member holds, are not read, so a named tuple is written
with its types alone. The strict build is clean and the tests pass, 179 and one skipped plain and
180 in the variant.

The check of what a module carries in the commit carrying this line, the third of the three, with a
named tuple's element names on the cheatsheet beside it. CarriesTests reads the README's section
Binding it in your engine, and the table of "What a module carries" names every fact its C# blocks
give with the check the fact's block calls and names no other fact; every commit the table names is
an ancestor of the head, read where the checkout holds its whole history and skipped with its reason
where it does not, and the workflow's checkout takes the whole history, which the version's patch is
counted from as well. A made README with a fact the table lacks, a row no block gives and a row
naming another check reads the three faults. The table named none of the commits that changed what
two facts read after their own, `75d77f2` for the link row's one word and `5bf6255` and `460a481`
for the cheatsheet's defaults and forms, which it names. The cheatsheet writes a tuple's element
names from the attribute the build keeps them in, a list in the order of a depth-first walk of the
member's tuples, each tuple's names before its elements', as `(Plan<TMesh, TTag>.Item Item, bool
Moved)` and `((int Low, int High) Range, string? Name)` beside an unnamed `(int, int)`; both lines
failed first, and the writer reads each element once since its reading takes names from the list.
The strict build is clean and the tests pass, 183 and one skipped plain and 184 in the variant.

A module's devlog of commits in the commit carrying this line, the first of the three by the
review's order. `devlog.py --commits` lists each day's commits under it, newest first, each by its
sentence, a message's first line under its subject or its two lines where it has two, as ``
`abc1234` The sentence. ``, and leaves out a commit that changes the review's files or DEVLOG.md
alone; the items done still stand above where a review records them. DevlogTests failed first on a
made repository of four commits, one sentence of two lines, one of the review alone and one of the
devlog alone, the two listed newest first and the two left out, and no list without the flag. The
README says it, and a module's command takes the flag. A clone of Landscape reads 19 commits over
its three days. One consequence is the review's to settle: the text fact reads DEVLOG.md, and the
histories of Levels, Modeling, Sky and Vegetation each hold one message line naming an engine
outside the assembly's form (git log --format=%B), which a devlog written with `--commits` would
carry into the text the fact fails; DEVLOG.md can be left out of the fact as the review's files are,
being written from the history, or those commits left out of the devlog. The strict build is clean
and the tests pass, 184 and one skipped plain and 185 in the variant.

A module's pin in the commit carrying this line, the second of the three, with the review's two
rules on an engine named in a commit's sentence. `scripts/pin.py` is run from a module's root by its
own session: it fetches `modules/Tools` and checks out the head given, `origin/main` where none is,
prints the commits the pin moves by, and names from the table of what a module carries at the old
pin and the new each fact new to the module and each whose row changed with the commits of its row
that the old pin does not hold, a change the pin holds already left unsaid. Where the module keeps a
CHEATSHEET.md it runs its test project's cheatsheet fact with `CHEATSHEET_WRITE=1`, and it writes
DEVLOG.md again with the new pin's devlog.py and `--commits`. It commits nothing, and a folder
outside git or a repository with no `modules/Tools` is refused. PinTests reads a made module pinned
at a made Tools' first commit moved to its second, a new fact, a changed one and one whose change
the pin holds, the devlog written, the pin moved and nothing committed, and the two refusals; the
first run failed first on the table's rows, whose last cell stands empty where nothing changed a
fact and which the row's pattern read as no row, as it would have read every row of this
repository's own table. On a clone of Landscape outside its folder, the module untouched, the pin
moves from `75d77f2` to `7b714d7`, nine commits, names the cheatsheet fact changed by `5bf6255` and
`460a481` alone, and writes the cheatsheet and the devlog again, in four seconds; the first version
named the link row's fact changed by `75d77f2`, the very pin, which the held commits mend. By the
review's word on the rule, `devlog.py --commits` leaves out a commit whose sentence names an engine
outside the form `3DEngine.<Name>` and counts it on its own output, and `commitcheck.py` fails such
a message in a repository under Modules other than Tools; both failed first, the devlog's on a made
repository of a sentence naming the assembly's form and one naming an engine, the check's on one
commit's message in a module, in another module naming its assembly and in Tools. The README says
both and AGENTS.md's scripts row names pin.py. The strict build is clean and the tests pass, 188 and
one skipped plain and 189 in the variant.

A commit's report in the commit carrying this line, the third of the three. `scripts/report.py`
prints a commit's block, every line read by the script and none by hand: the hash and the sentence,
the files by kind and the lines added and removed, the facts and the theories' rows of data the diff
adds to C# files counted on added lines alone, a theory's own attribute not counted and an
attribute's form in a page not read, the counts of a test run read from a TRX file, the one `--trx`
names or the newest under the repository's TestResults folders, and `commitcheck.py`'s exit code
with its FAIL and WARN lines counted. By the review's word a count it could not read is said unread
with why, a TRX missing or broken, and a commit git cannot read gives 2. ReportTests failed first,
the script being set aside, on a made commit of a test file, a script and a page whose block reads
its sentence of two lines, three files of three kinds, 17 lines, two facts and two rows, a TRX's
five counts and a check that fails a message without the marks, and on the unread TRX in both forms
and the unread commit. The README says it and AGENTS.md's scripts row names it. The run of
`eb6ded6`, 44375686499, is green on Linux, Windows and macOS, read with run.py. A TRX file is read
by its results' outcomes, and by its counters only where it holds no results, since the first block
of this commit read 191 total and 190 passed and nothing else: dotnet test counts the skipped test
in no counter, its notExecuted 0, while its result's outcome is NotExecuted; a third test failed
first on a made TRX of that form. The strict build is clean and the tests pass, 191 and one skipped
plain and 192 in the variant.

The index read before a commit in the commit carrying this line, the first of the three by the
review's order. `commitcheck.py <repo> --staged [--message=<file>]` reads what the index holds by
the rules a commit is read by, printed as `staged`: its files against HEAD by `git diff --cached`,
its pictures as the index holds them, the identity git would give the commit by `git var`, and the
message in the file named, the one `git commit -F` then takes, the message's checks moved whole into
a function of their own that both read. Where no message is given it is not read and a note says so;
nothing staged is said with 0, and a commit given with `--staged` is refused with 2.
CommitCheckTests failed first on three made repositories: a staged script with a comment past 100
columns and a message without its marks, both failing while a work tree's change unstaged is not
read; no message given and nothing staged; and a staged picture on a dark ground, the work tree's
copy made white after it was staged. Run on this batch before its commit it found a comment line of
101 columns in the new tests in 0.09 seconds, mended before the commit. The README says it in its
section for a session and in the check's own. COMMITS.md's pass before a commit could name it, which
is the review's text to change. The strict build is clean and the tests pass, 194 and one skipped
plain and 195 in the variant.

A mend of the devlog's commits in the commit carrying this line, found by three modules on their
first pin.py. `devlog.py --commits` leaves out a commit whose line on the page would fail the check
an added line of a page gets, read by `line_faults` as any page's line since the check leaves
DEVLOG.md itself unread, and by the review's word reads "the bridge" as naming an engine for this
leave-out alone; each count is printed on its own line of the output and never into the page. The
devlog imports the check from beside it, so PinTests' made Tools carries commitcheck.py too.
DevlogTests failed first on a made history of a sentence kept, one with the cheatsheet in two words,
one with a middle dot and one naming the bridge, the first alone listed. Clones of the three modules
read: Materials 1 left out by the line check of 26 kept, RadianceCascades 1 of 22, and Vegetation 6
naming an engine of 18, its five of the bridge and the one found before, and none of the three pages
holds the two words, the bridge or a middle dot. The staged check found a comment of 102 columns in
the new test before the commit. The strict build is clean and the tests pass, 195 and one skipped
plain and 196 in the variant.

The sheets folder in the commit carrying this line, the second of the three. `sheets/` holds
sheet.py and chart.py, moved with git from `scripts/`, their examples moved from `examples/`, their
tests and a page of their own, so a copy of the folder alone is a repository of them with Pillow its
one need. The page opens, as the review asked, with a picture and the three lines that make it, `pip
install pillow`, an `echo` of a one-line layout into first.json and `python3 -I chart.py first.json
first.webp`, then the page a stranger had, the examples run from the folder's root, the two layouts'
keys moved from Tools' README, and how the tests run. The tests are Python's unittest, the C# facts
of SheetTests, ChartTests and ExamplesTests ported assertion for assertion to ten tests and deleted,
the drawing ones skipped where Pillow is missing as NeedsPillowFact skipped them, so Windows and
macOS run the page's tests alone, as they did. Breaking a copy of each script, a label's side in
chart.py and sRGB's exponent in sheet.py, fails its tests, which a port passing at once does not
show. SheetsTests holds two facts that failed first, the folder missing: the folder's tests pass in
it, and on a copy of the folder alone outside any repository they pass with no file of it naming a
parent, `scripts/`, `.github/` or Tools; a third, that the page opens with its picture and its
lines, failed first in C# and moved into the folder's tests, so the folder holds it. Tools' README
holds one short section pointing at the folder's page, and AGENTS.md's `scripts` row drops the two
and its `examples` row is the `sheets` row, as N 1.5 asks. Lines outside Tools that name where
sheets are made: none in the engines' NORM.md or AGENTS.md or the modules' AGENTS.md or NORM.md;
Landscape/build/reading.cs:4 names "Tools' sheet.py" with no path, true after the move;
Landscape/.github/REVIEW.md:213 names it in a Reply of the past; and no script outside Tools calls
either by its path. The strict build is clean and the tests pass, 188 and one skipped plain and 189
in the variant, the nine C# facts gone and two come.

A mend of the report in the commit carrying this line, by the review's word. report.py reads a
folder of tests whatever its case, so `sheets/tests` stands with `Tools.Tests` under tests and not
under scripts, and counts beside the C# facts the test functions, `def test_`, a commit adds to
Python files of tests, a like-named function outside a folder of tests not counted. ReportTests
failed first on a made commit of a Python file of two tests and a helper under a lowercase tests
folder and a script holding a function named like a test. Read again, `1452dbe` gives tests 9 and
scripts 2 where it gave tests 4 and scripts 7, and 10 test functions. The strict build is clean and
the tests pass, 189 and one skipped plain and 190 in the variant.

The reading of the three big repositories' pictures on a dark ground, the third of the three, in the
commit carrying this line alone, the page and its pictures in `.github/dump/2026-11-18/`,
20-32-reading-334.md with a row a reading. The sweep's 334 are 3DEngine's 83, BevyCSharp's 127 and
Voxel-Sandbox's 124, Voxel-Sandbox being its own repository inside 3DEngine's folder. Each was
looked at on twelve contact sheets sheets/sheet.py composed, thirty to a sheet, beside its caption,
of which 254 have one. None can be remade from tracked files alone. Every reading but one is frames
of an engine or a game, alone or edge to edge, sheet.py's form with the HUD's text inside the
frames, and no reading's frames are tracked, so each waits on a GPU's run of the tracked program to
capture them again: 220 of a tracked scene whose caption gives its settings, 3DEngine 35, BevyCSharp
113 and Voxel-Sandbox 72; 80 of a tracked scene whose file's name alone says what they show, 16, 12
and 52; and 33 of Sponza, 32 of 3DEngine's and one of BevyCSharp's, a scene fetched by a tracked
command and not held. The one chart, BevyCSharp's item-137-near-wall, is chart.py's form, but item
137's record in REVIEW-ARCHIVE.md keeps the ranges and four points of its series alone. By their
words 70 show two states, a remake capturing at two commits, and seven set a bounce room beside its
path-traced reference, which both engines track as PFM but at 80 by 45 averaged, where the readings
show it whole. Two findings of chart.py itself, drawing the counts: a bars panel draws no legend
unless the layout gives one, though each series has its label, and a whole count reads 1.00 or 35.0,
every value given three significant figures; neither is mended here, each a small item if the review
wants it. The tests are as at `0d9a2a7`, nothing tracked but this line changing.

chart.py's two mends in the commit carrying this line, by the review's word, both in the sheets
folder's own tests and each failing first. A bars panel's labeled series stand as its legend over
its plot, a swatch of each series' color and its label, printed as `panel N legend S at`, where the
layout gives no legend of its own, which a sheet's legend still stands for alone; the test failed on
no legend's line, reading after the mend each swatch's color at its printed place, its label's ink,
the legend clear above the plot and no panel's legend under the sheet's. A value JSON holds as a
whole number is labeled whole, 35 and 1 where they read 35.0 and 1.00, and one written with a point
keeps three significant figures, 2.0 reading 2.00, a bar's printed line ending `labeled` and its
text; the test failed first for want of the printed text, and with it added on the figures
themselves, 35.0 and 1.00 where 35 and 1 were expected. The four chart examples draw pixel for pixel
as they are kept, the bars example giving its sheet's legend and holding no whole value under a
label, so no picture changes. The page says both. The folder's tests run 12, and Tools' strict build
is clean and its tests pass, 189 and one skipped plain and 190 in the variant.

Pillow on all three systems in the commit carrying this line, the first of the three by the review's
order. The workflow installs Pillow 12.3.0 from PyPI on Windows and macOS, beside Ubuntu's from its
archive, into the Python the tests find, python3 and then python as Programs.Python tries them,
letting pip past its guard where that Python is the system's, as Homebrew's is, and printing the
version and the interpreter it went into. 12.3.0 is the version here, so a fault the workflow shows
is one this desktop can draw again; pip found its wheel. SheetsTests holds a fact run in the
workflow alone, by an attribute that reads GITHUB_ACTIONS and skips elsewhere with its reason, which
fails where Pillow is missing or any of the folder's tests is skipped; it failed first with
GITHUB_ACTIONS set and a Python without Pillow first on the path, a venv made without packages,
saying python3 has no Pillow, and passes with this desktop's. Windows' and macOS's jobs will draw
for the first time, so their run is the reading, and a difference of fonts or line ends would show
there first. The reading's label is corrected on its dump page, the 80 with no caption marked as
coming in with a commit whose sentence tells of other work, each row giving that commit and its
sentence; the counts chart there keeps the old label in its legend. The strict build is clean and
the tests pass, 189 and two skipped plain and 190 and one skipped in the variant, the new fact the
skipped one in both.

Captions at the commit's check in the commit carrying this line, by the reworded N 4.8. A picture a
commit adds or renames under `.github/assets/readings` or `.github/devlog` fails where the
captions.md beside it, as the commit or the index holds it, gives it no line, the failure naming the
line that would satisfy it, `<name> | <one sentence>.`, or where its line does not end in a full
stop; each repository's place is the picture's own folder, readings beside readings in the engines
and `.github/devlog/captions.md` in the game. devlog.py no longer captions a picture with its
commit's sentence, a picture with no line standing without a caption and counted on its output.
Three tests failed first: CommitCheckTests on a picture with no caption in either folder and on one
with its line and one whose line has no full stop, and DevlogTests on a picture whose commit's
sentence tells of other work, which the page no longer shows. Tools' own NORM.md still gives the
fall back at its line 76, and the reworded text has not reached this tree, so it stands for the
review to carry. The run of `457f3e5` is read in the report. The strict build is clean and the tests
pass, 192 and two skipped plain and 193 and one skipped in the variant.

The commit's run in its report in the commit carrying this line, the third of the three, with the
reworded N 4.8 the review left in this tree. `report.py --run` ends the block with the commit's
workflow run on GitHub, its number, its state and each job's conclusion and time as run.py gives
them, read through gh by the commit's hash from the origin's address, in either of GitHub's forms,
or says why it was not read, no origin on GitHub or gh unreached, or that the commit has no run yet;
its tests replace gh by answers REPORT_ANSWERS names, as status.py's do. ReportTests failed first on
a made repository with a run of two jobs, one failed, and on no remote, no run and gh failing, with
one fault of the test's own mended before the script was written, its recorded run saying success
where the expected line said failure. Read live, `457f3e5`'s block gives run 44382532549 with its
three jobs at 56 s, 1 min 29 s and 38 s, as run.py read them. The run of `457f3e5` drew on all three
systems, Pillow 12.3.0 in Windows' toolcache Python 3.12.10 and, past Homebrew's guard as the step
foresaw, in macOS's Python 3.15, the workflow's fact skipped in no job. The strict build is clean
and the tests pass, 194 and two skipped plain and 195 and one skipped in the variant.

The review's archive in the commit carrying this line, with the review's REVIEW.md, 40 KB, and the
62 older reply paragraphs it moved whole into `.github/REVIEW-ARCHIVE.md`, as left in this tree. The
archive is left out of the text fact and of the commit's check in a module as REVIEW.md and NORM.md
are, ModuleChecks' pattern and commitcheck.py's taking `REVIEW-ARCHIVE` beside `REVIEW`, and the
devlog's commits already leave a commit of it alone out; the two tests that leave the review's files
out failed first with an archive naming an engine beside REVIEW.md, and the remark and the README
say the archive. AGENTS.md's documents table has its row. The text fact's check changed, so its row
in the README's table of what a module carries names this commit in the next, a hash being named
only once it stands. The strict build is clean and the tests pass, 194 and two skipped plain and 195
and one skipped in the variant.

A review's archive as a script in the commit carrying this line, the first of the three.
`scripts/archive.py <repo> [--under=<KB>]` moves a review's oldest replies whole from REVIEW.md into
REVIEW-ARCHIVE.md until the review stands under 40 KB, a reply by the review's word its first
paragraph and the indented, listed, tabled or fenced paragraphs after it, a fenced block's blank
lines kept inside it, nothing above `## Replies` touched, the archive made where none stands and the
moved replies following those it holds; it commits nothing and a review or a Replies section missing
gives 2. Replayed on REVIEW.md as it stood at `6ac6571`, it moves 55 replies, the review's 62
paragraphs, and writes an archive whose replies are byte for byte the review's hand move, 60,072
bytes, and a REVIEW.md that is the review's as it left it before the Reply after. ArchiveTests
failed first on a made review of 30 replies, one with a list, a table and an indented line after its
first paragraph and one with a fenced block, and a third test sets the limit inside the reply with
the list, so a move by paragraphs stops in it; with the reply's rule broken that test fails, where
the first two alone passed, every early reply moving either way. status.py says a review from 80 KB
and names the script, its tests failing first at 81 KB and on the line's new words. The text fact's
row in the table of what a module carries names `70543a2`. The README has a section for the script
and AGENTS.md's scripts row names it. The strict build is clean and the tests pass, 198 and two
skipped plain and 199 and one skipped in the variant.

The table of what a module carries kept up in the commit carrying this line, the second of the
three. CarriesTests fails a commit that changes the code of `Tools/ModuleChecks.cs` or
`Tools/Cheatsheet.cs` after the table stood, a line of a diff neither blank nor a comment, which the
table names nowhere, a row or a new line under it taking a commit that changes no fact's reading; it
is skipped where the history is shallow. The files are read and not the methods, since `70543a2`
changed the text fact through a pattern its check calls and not the check's body, which `git log -L`
on the method would have missed. The fact failed first on the history itself: `dc117ac` changed the
cheatsheet's writer, the tuples' element names, and no row named it, so pin.py told no module its
cheatsheet would change; the cheatsheet fact's row names it now. The strict build is clean and the
tests pass, 199 and two skipped plain and 200 and one skipped in the variant.

The sweep's count of uncaptioned pictures and a mend of the report in the commit carrying this line,
the third of the three. sweep.py, and daily.py through it, warns of each standing picture under the
readings or `.github/devlog` that its folder's captions.md gives no line, counted with its places,
where the commit's check refuses one added; SweepTests failed first on two such pictures, one in
each folder, beside one captioned, and Voxel-Sandbox's sweep counts 53. `report.py --run` gives a
job not yet ended no time where it gave 0 s, gh giving it an end in the year 1; ReportTests failed
first on such a job. The strict build is clean and the tests pass, 200 and two skipped plain and 201
and one skipped in the variant.

Handoff for a fresh session, written as the review asked. Tools holds the library `3DEngine.Tools`
(ModuleChecks, Cheatsheet, ShellForms, DocumentCopies), which the modules' facts call, and the
scripts the repositories run from `scripts/`: devlog.py, run.py, commitcheck.py with `--staged`,
hooks.py, status.py, pin.py, report.py with `--run`, sweep.py, daily.py, rewrap.py, rewrapcheck.py
and archive.py, each with a section in README.md; `sheets/` holds sheet.py and chart.py with their
own page and Python tests and stands alone as a copy. Read first AGENTS.md, then REVIEW.md's head,
its Now list and the last Replies, then README.md's section for a session and its table of what a
module carries, whose rows CarriesTests holds to every commit that changes the checks' code. The
working form: a failing test before each mend, `commitcheck.py . --staged --message=<file>` before
each commit, the build and the tests both plain and through build/variants.sh, `report.py . HEAD`
for the block, and a short report to the review, which pushes. Nothing is open here: the three items
are done, the 80 or so uncaptioned standing readings of the big repositories stand as their day's
record, counted by the sweep, and no work is proposed until the review or the owner brings it.

A day's use in the commit carrying this line, the hour's step. `scripts/usage.py [<date>]` sums the
transcripts' usage by session and by hour, a request counted once by its requestId, its context its
input, cache written and cache read; UsageTests failed first on made transcripts of a repeated
request, another day's, a subagent's, a broken line and a second session. Today's read: 12 sessions,
8,441 turns, 3.36 billion tokens of cache read, each session's mean context between 223,500 and
523,002. Tests pass both ways.

usage.py's hours in the commit carrying this line, the hour's step: each hour on a line of its own
in local time, the date read in local time, and `--since=<hour>` keeping the hours from it on;
UsageTests failed first with TZ an hour east of UTC, the other day's request falling on the day
before in local time and `--since=17` leaving the earlier hour out. Tests pass both ways.

Stale waiting marks in the commit carrying this line, the hour's step. status.py names each mark in
the lock's waiting folder older than ten minutes, `--waiting` giving the folder and the lock's the
default, as `waiting <name>, <repository>'s pid <pid>, <age> min old`, with whether its process
lives read from `/proc` on Linux and not read elsewhere, since `os.kill` with signal 0 on Windows
sends Ctrl and C. StatusTests failed first on a made folder of a mark 25 minutes old whose process
is gone and one made just now, which is not named. Tests pass both ways.

The repositories' new names in the commit carrying this line, by the owner's word. LinkRowFault's
row links to `EggyStudio/3DEngine.<Name>`, the title still `# <Name>`; status.py reads a repository
under Modules whose remote gives no name on GitHub as `EggyStudio/3DEngine.<folder>`, the engines
and the game as they were; pin.py names a submodule still named by Tools' old address by its new
one, in `.gitmodules` and in the checkout's origin where that named the old; and the README's row,
its submodule commands and its words follow. The tests failed first on the row, the default name
read from a bare remote, and the submodule's old address; the grep for the old addresses finds none
outside the review's files and the devlog.

The guard of the table mended in the commit carrying this line, after `13ca744`'s run failed it on
all three systems: the guard read the head as a commit to be named, which a commit changing a check
cannot do of itself, and passed here only because the commit did not yet stand when it ran. It now
lets the head stand unnamed and holds every commit before it, read through a helper a made history
tests; that test failed first with the head held, the made head changing a check, and passes with it
apart. The link row's row names `13ca744`.
