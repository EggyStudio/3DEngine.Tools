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

Decision 6's first Reply, this file read against the tree at `67e6e30`, the last commit here and on
origin.

**Done.** Items 1 and 2 at `e28b3b4` and `13c8202`, and item 3's devlog generator and run reader at
`7532128` and `321de49`, as the head says. The head does not name `67e6e30` of 2026-11-17, devlog.py
reading its rewrites, patterns and undated day from the file `--fixes` names, which is on origin
with its run green (44111799256). The strict build is clean and the 3 tests pass (dotnet build
Tools.slnx -warnaserror, dotnet test Tools.Tests).

**Open.** Item 3's sheet composer, and the check item 3 asks of each repository's copies, which has
no part. Item 4 has none, neither engine's `.gitmodules` naming Tools. DEVLOG.md ends at 2026-10-31
without item 3's parts, and is regenerated with the next commit.

**What the module holds.** The scripts a session runs from this checkout over any repository,
Python 3 under `-I` with Pillow where a picture is written, taking paths and JSON files, devlog.py,
run.py and the sheet composer. The sources of STYLE.md and COMMITS.md, the files under `.github`,
which the six other modules carry, COMMITS.md to the byte in all six and STYLE.md in five, Levels'
adding the middle dot's paragraph that 3DEngine's holds as well, and which the engines carry with
paragraphs of their own (50 and 196 lines changed, `diff` of this STYLE.md against 3DEngine's and
BevyCSharp's). NORM.md is the review's (N 7.3) and no source here. The library holds what both
engines' tests run over their own checkouts and hold twice today, the shell forms only GNU's tools
or bash 4 read, 3DEngine's ScriptTests reading seven and BevyCSharp's ten, and the copies' check. No
editor and no editor shell, which wait on the owner's word (3DEngine Decision 114). The texture
encoders Modeling's and Vegetation's `build/textures.py` share, 24 functions of the same name with
BC7's and BC5's encoders, the KTX2 writer and the mipmaps among them, stay there, since no module
takes another, until the review decides whether one does.

**The interface.** The module needs nothing of a renderer, a world or a scheduler. It reads a
checkout's files through System.IO, a checkout being plain files and no engine's asset store, so it
declares no interface for an engine to implement. An engine binds it by the submodule at
`modules/Tools` referenced from its test project alone, since nothing an engine ships calls it, one
test of its own calling the library with its root and the words its copies put for the module's,
and the scripts run by their path under `modules/Tools/scripts`.

**The first three commits.**

1. The sheet composer, `scripts/sheet.py`, item 3's last part, begun with this Reply. A JSON layout
   of rows of panels on a white ground, a heading a column and a title a row in dark text. A panel
   is a capture, PNG or PFM, cropped, the PFM's linear light at a gain, or at the gain that puts a
   reference's mean at a value, in sRGB; the signed difference of two in red where brighter and
   blue where darker, full at a share of the reference's mean; or the largest channel's difference
   times a factor. A box is drawn only with the mean it measures inside, printed beside it and on
   the terminal. SheetTests composes a sheet from two PFMs and a PNG it writes and reads its white
   ground, a panel's pixels where placed, the difference red and blue where it made a frame brighter
   and darker, and the box's mean as it computes it, skipped with its reason where Python or Pillow
   is missing (N 3.2). The workflow installs `python3-pil` on Ubuntu as 3DEngine's captures do, so
   it runs there. A sheet the test makes goes to `.github/assets/readings` (N 4.8).
2. The shell forms, `ShellForms` in the library, BevyCSharp's ten with 3DEngine's seven among them,
   a line's comment left out, the scripts a workflow's Windows or macOS jobs run and those they
   call, and every script of a checkout. The engines' cases of each form come with it, and a test
   reads the module's own `build/version.sh`, which gives N 6.6 its test here.
3. The copies' check, `DocumentCopies`, STYLE.md and COMMITS.md carried in the assembly. Every
   paragraph of a source, with the copy's words for the module's put in, stands in the copy in its
   order, and the copy adds paragraphs of its own between; a paragraph it lacks or changes is named
   by its first words. Tests of a copy that adds a paragraph, changes a word, drops one and puts its
   words in. The source takes the middle dot's paragraph 3DEngine and Levels hold, if the review
   agrees.

Then item 4, each engine's binding, deleting its own table of forms and taking the check, the
paragraphs it changed set apart as its own or offered to the source. Item 3's Shared line goes by
message to each engine's session and to the review as a part lands, this session writing here
alone.

Item 3's last part, the sheet composer, in the commit carrying this line, with N 4.11's test.
`scripts/sheet.py` as the first commit above gives it, its layout's keys in the README, a box's
reading to four significant figures, since the first sheet read the floor of 3DEngine's room at
0.0005 and 0.0004 to four decimals where 0.0005032 and 0.0004174 tell the two apart. The sheet of
that room is the devlog's picture, `.github/assets/readings/2026-11-18-item-3-sheet.webp`, composed
in a second from the PFMs of four 1280 by 720 frames. SheetTests composes a sheet of a capture with
two boxes, a signed difference and a difference from two PFMs and two PPMs it writes, and reads the
white ground, a capture at 0.25 shown as 137, the red, blue and white of the signed panel, the
difference's 215 and the boxes' 0.5 and 0.375; it runs where Python 3 imports Pillow and is skipped
with its reason elsewhere, and the workflow installs `python3-pil` on Ubuntu. CommentTests reads
every comment line of the C#, Python, shell, YAML and MSBuild sources and failed on 14 past 100
columns, in the scripts, the tests, both project files and the workflow, which are wrapped. The
strict build is clean and the tests pass, 15 where there were 3 (dotnet test Tools.Tests). DEVLOG.md
is regenerated. AGENTS.md's DEVLOG.md row carries its link at `67e6e30`, so nothing there
changes, and its scripts row, which names devlog.py and run.py, waits on the owner's word to name
sheet.py (N 7.4).

The sheet's layout mended in the commit carrying this line. A row's title stands first on a line of
its own in a type 1.3 times the labels', the panels' labels under it, the first row's taking the
columns' headings where a panel has none, and the signed difference lies on black, red and blue
rising from it, where its ground was white. SheetTests failed first on the signed panel's ground,
white where black was read, and reads the title's line above the labels', the labels' above the
panels and taller type for the title, three of the signed panel's corners black, and the white
ground left of, right of and under every panel. The reading is composed again, AGENTS.md's scripts
row names sheet.py, and the README says the layout. The strict build is clean and the 15 tests pass.

The second commit above, the shell's forms, in the commit carrying this line. `ShellForms` holds
BevyCSharp's ten forms with what reads in each one's place, `In` and `Code` for a line with its
comment left out, `Find` over scripts given by their paths, `Scripts` for every `.sh` of a checkout
less the folders a build fills and those a caller names, and `RunElsewhere` for the scripts the
workflows' jobs on any system but Ubuntu name and those they name in turn, a matrix's job counted
among them and a launcher with no `.sh` named by the caller, as `e3d` and `bcs` are. ShellFormsTests
carries both engines' cases of every form, 24, reads a made checkout whose macOS job runs a script
that runs another and a launcher while an Ubuntu job's script and one named in a comment stay out,
and reads the module's own scripts, which gives N 6.6 its test here. Run over the engines at their
heads, BevyCSharp's 33 scripts and the 2 its Windows and macOS jobs reach, and the 18 3DEngine's
reach, hold no form, as their own tests have it; every script of 3DEngine, 95, holds 2, in
`Voxel-Sandbox/build/fetch-slang.sh` and `Voxel-Sandbox/build/play.sh`, which its binding meets if
it reads every script as BevyCSharp does. The strict build is clean and the tests pass, 41 where
there were 15.

The third commit above, the copies' check, in the commit carrying this line. `DocumentCopies`
carries STYLE.md and COMMITS.md in the assembly, `Source` giving one by name, and `Differences`
names each paragraph of a source a copy lacks, changes or holds out of order by its first eight
words, the copy's words put in as whole words, a paragraph read with its lines joined and a fenced
block with its lines kept. The source STYLE.md takes the middle dot's paragraph. DocumentCopiesTests
reads the carried sources as the checkout's files, a copy that adds paragraphs and wraps its lines
again holding the source, a changed word and a dropped paragraph named, a changed line of a fence, a
paragraph out of order, the words put in and a name the module carries no source of refused. Read
against the copies at their heads, Levels' hold their sources; Landscape's, Modeling's,
RadianceCascades', Sky's and Vegetation's COMMITS.md hold theirs and their STYLE.md lacks the middle
dot's paragraph alone; with `engine` put for `module`, 3DEngine's STYLE.md differs in 5 paragraphs
and its COMMITS.md in 9, BevyCSharp's in 29 and 9, which each engine's binding settles, its own text
set apart or offered to the source. The strict build is clean and the tests pass, 48 where there
were 41.

The fourth commit, the commit's check, in the commit carrying this line. `scripts/commitcheck.py` is
the review's check taken whole, the forbidden names read from FORBIDDEN_NAMES alone and printed
nowhere, with these mends. The message is read from the commit object, where `log --format=%B`
added a line end of its own and the count of lines stood on it. The sentence fails on a colon
inside it, a spaced hyphen and an en dash, and prose on an en dash, which STYLE.md forbids beside
the em dash. A comment line past 100 columns fails, N 4.11 being a rule, where it warned, and a
line holding an address alone is free as the rule has it. An MSBuild comment is read, over its
lines in an MSBuild or XML file alone, since the first reading of this repository's commits took a
C# string naming `<!--` as the opening of one and failed two lines of code; a test failed first on
it. A repository's first commit passes, where its no parents failed; a merge still fails. A bad
revision or a pattern that does not compile stops with a sentence, where it ended in a traceback,
and the thumbnails' folder is made where it is missing, under the system's temporary folder where
none is given. CommitCheckTests makes a repository of one commit for each case, a commit in the
form, one without the marks, one without its full stop, one with a line after its sentence, a
comment line past 100 columns beside an address alone, a line of code naming the XML mark, a
made-up name given in the environment and the warning where none is, and a picture on a black
ground, skipped where Pillow is missing. Read over this repository's commits from `1c0015a` to
the head, none fails. The README says it and AGENTS.md's scripts row names it. NORM.md, naming
eight modules, goes in with it, and STYLE.md and COMMITS.md, the sources, name none. The strict
build is clean and the tests pass, 56 where there were 48.

The tree's sweep in the commit carrying this line, with two mends of the commit's check, each with
a test that failed first. `scripts/sweep.py` reads a checkout's tracked files as they stand by the
rules `commitcheck.py` reads added lines with, which it imports from that script, its line's and
picture's rules made functions for both, and prints a count a rule with the first ten places of
each and an exit code of 1 where a failing rule is broken. Over 3DEngine's tree, read and written
nothing to, 3,064 tracked files, 1,948 of them text and 134 pictures, took 2.4 to 3.0 seconds at a
load of 12, where the first version took 3.3 at rest, the pictures' decoding and the patterns'
searches each half of it (`python3 -I -m cProfile`); each list's patterns are searched as one
before each alone, the spelling's pattern holds no lookbehind, the pictures decode on threads while
the text is read, and a file's comment mark is found once. It reads 5,546 comment lines past 100
columns there, 22 British spellings, 7 of "word" for four bytes and 83 pictures whose ground is not
white, with 304 warnings of the comments' patterns and 9 pictures with no title; with no names
given it says so. SweepTests makes a tree that keeps the
rules, one breaking each rule with twelve long lines of which ten places are printed, an untracked
file left out and a made-up name in a file and in a path counted and printed in neither, which
failed first on the path, one with no names given, and one with a dark picture beside a white one.
The first mend is the review's case arm. A comment's mark is its language's, `//`, `/*` and a
block's `*` for the C family, `#` for Python, shell, YAML and TOML and `<!--` for MSBuild and XML,
where one pattern for all read a shell's `*)` as a comment; RadianceCascades'
`build/fetch-slang.sh:68` reads as code. The second is the check's own, a Markdown line held to no
width whatever its mark, where `2a8d768` failed a heading or a list's line past 100 columns. The
sweep writes no compiled copy of the script it imports, and the ignore file leaves those out. The
three marks, the dashes and the middle dot stood as characters in commitcheck.py, devlog.py and
three test files, the marks unseen in an editor and the dashes against STYLE.md's check of the C#,
and are written as escapes. The strict build is clean and the tests pass, 62 where there were 56.

The README's section For a session in the commit carrying this line, ten lines under the opening
paragraph, saying how a session runs `commitcheck.py` on its commits before reporting them, with the
names in the environment and the unpushed commits read where no range is given, and what a FAIL, a
WARN and a note mean.

The reference check's form and the README's section Binding it in your engine in the commit
carrying this line. `Tools.Tests/ReferenceTests.cs` reads the built library's assembly references
and the packages the test's dependency file records for it, followed to those they bring, against
a list of refused names, the engines' assemblies and the windows, graphics APIs and bindings a .NET
program draws through, each matched as a whole part of a name between dots and dashes; a module
copies the file changing the library's name alone. Its cases refuse the engines' own references,
3DEngine, BevyCSharp, SDL3-CS, Vortice.Vulkan and Twizzle.ImGui-Bundle.NET, with Silk.NET.Vulkan
and Raylib-cs, and pass System.Runtime, System.Reflection.Metadata and xunit.core. The README's
section, where Taking it into an engine stood, says that the module needs nothing of an engine and
declares no interface, references the library from a test project alone, and names the two tests
an engine adds, the scripts' needs and the reference test's form. The strict build is clean and the
tests pass, 74 where there were 62.

The comment patterns narrowed in the commit carrying this line, as the review read the sweep's
output. A comment tells what the code was by used to, no longer, formerly, previously, was once, had
been, before this or an earlier version, where a plain earlier, as an earlier command, and a
regression counted. A comment in a workflow or a build script, a `.sh` or `.ps1` file or Python
under `build` or `scripts`, is read for a run's figures with a unit and its dates alone, and the
words of a run, runner, a CI run or a run's number, are read for elsewhere. SweepTests failed first
on a made tree with a plain earlier, an earlier version, a workflow's runner, a workflow's 8 ms and
a C# comment's runner, two warnings of each rule expected and three and one read. Over BevyCSharp
at its head (python3 -I scripts/sweep.py ../../BevyCSharp), the first rule's warnings fall from 99
to 47 and the second's from 147 to 93. Most of those left are plain as well, is used to, before this
frame, had been allocated and the generator's runner class, so a narrower reading is proposed, used
to after no form of be, before this followed by change, commit or version, had been left out, and
runner read in a test's comment alone. The strict build is clean and the tests pass, 75 where there
were 74.

The mend of `d3add46`'s red run on Windows (44345093619) in the commit carrying this line, with
both causes the review read. The lines arrived ending in a carriage return and the tests split at
the line feed alone, so the count's line was not found; the tests split a program's output at any
line end by one helper, in SweepTests and SheetTests, the checks of CommitCheckTests reading the
whole output. And Python wrote the pipe in cp1252, the dash's byte 0x97 read by .NET in the
console's code page as ù; every script writes UTF-8 whatever the system gives a pipe, and the tests
read a program's output as UTF-8. The long-line rule counted the twelve there as well, read from
the file the test writes with line feeds alone, which Python's text mode reads the same on every
system, and its line was the one the carriage return hid. A test of each script prints a place
holding the em dash under a Latin-1 locale on Linux, Windows' own encoding there, and reads it back
whole; both failed first, sweep.py and commitcheck.py stopping on the dash they could not encode.
The strict build is clean and the tests pass, 77 where there were 75.

The comment patterns narrowed further in the commit carrying this line, as the Reply above proposed
and the review answered, both rules warning and never failing. A comment tells what the code was by
used to after no form of be and no comma, before this ahead of a change, a commit or a version, and
had been is left out; the runner is read for in a test's comment alone, where it is a run's
machine, and the generator's runner class and a pool's worker elsewhere are left out. SweepTests
reads each form with a line that tells of the code or a run and a line of the same words that does
not, the second warned of nowhere, the used to after a form of be, after a comma, the before this of
a frame, the had been allocated and a library's runner, each of which failed first. Over the
engines at their heads (python3 -I scripts/sweep.py), BevyCSharp's warnings of the first rule fall
from 47 to 2, an earlier version of a file and a pointer previously returned, and of the second from
93 to 35, and 3DEngine's from 104 to 37 and from 89 to 35. The strict build is clean and the tests
pass, 82 where there were 77.

The names in the commit carrying this line, as the owner set them for every module. The library's
project sets its assembly and package id to `3DEngine.Tools`, still not packed, and the tests'
assembly is `3DEngine.Tools.Tests`; the folders, the project files and the namespaces `Tools` and
`Tools.Tests` stay. LibraryTests loads the assembly by its new name and reads that it holds
ShellForms, ReferenceTests' Library is `3DEngine.Tools` with its comment saying a module sets it to
its own assembly's name, and the README's section Binding it in your engine names the assembly and
the namespace. Built from empty output folders, the strict build is clean and the 82 tests pass.

The README's opening paragraph in the commit carrying this line, saying what holds. The module is a
library and a set of scripts that any engine takes as a git submodule, naming what it holds, the
devlog's generator, the sheet composer, the run reader, the commit's check and the tree's sweep,
the shell's forms and the documents' sources with their check, and it reads files alone, so it
references no engine, declares no interface and is taken by an engine of any kind as it is. The
texture recipes and NORM.md, which it named, are not held here. AGENTS.md's opening paragraph says
the same as the README's did, the texture recipes and NORM.md among what the module holds and the
interfaces it declares, and waits on the owner's word (N 7.4).

The sweep's picture rules read on 3DEngine's tree at its head, read and written nothing to, every
picture under `.github/assets/readings` by `picture_faults` from `scripts/commitcheck.py`, the
month by each name's date, all 134 named by one. 3DEngine's NORM.md took the white ground into
N 4.9 on 2026-10-31 (git log -S"on a white ground" -- .github/NORM.md).

| folder | month | pictures | ground not white | no title |
|---|---|---|---|---|
| `.github/assets/readings` | 2026-10 | 62 | 60 | 9 |
| `.github/assets/readings` | 2026-11 | 72 | 23 | 0 |
| all | | 134 | 83 | 9 |

By day, every picture before 2026-11-02 but two of 2026-10-31 stands on a dark ground, 2026-11-02
holds 12 of 26 and 2026-11-04 3 of 6, and from 2026-11-05 to the head 2 of 25 do, the pair
`2026-11-15-item-277-fetch-before.webp` and `-after.webp`, full frames of the atrium at 800 by
450 with the scene's own text over them and no ground round them. The 81 pictures of the days to
2026-11-04 are older than the rule's reading in the sessions and read as old pictures; the pair of
2026-11-15 is the one to judge, a plain capture whose white ground N 4.9 asks only of a pair or a
collage if a plain capture is read apart. The 9 with no title are all of October, 2026-10-23 to
2026-10-31.

The comment rewrapper in the commit carrying this line, with the table above. `scripts/rewrap.py`
fills again each paragraph of comment lines that holds a line past 100 columns, the run of lines of
one mark and one indent, in `//` and `///` comments of the C family, `#` comments of Python, shell,
YAML and TOML and `<!-- -->` comments of MSBuild and XML; a blank comment line, a documentation tag
alone, a list's item, whose lines go on under its text, and a line indented further end a
paragraph and keep their lines, a tag is one word with the text it touches, a comment after code is
left, and a word longer than the width stands alone and is counted as left. A file is refused and
left as it is where its lines of code or its comments' text, split at blanks alone, would differ.
Without `--write` it prints each file and the counts and changes nothing. RewrapTests reads a dry
run that changes no file, a summary with a `see` tag, a list's item and a line comment filled with
an indented line, a blank comment line and a comment after code kept, a lone `<` kept, a Python
comment under a shebang and an MSBuild comment filled, every line within the width, an address
standing alone and counted, and a copy of the script made to drop a paragraph's last word refused,
its file unchanged. The check splitting at blanks alone came of the first version, whose word
pattern dropped a `<` that opens no tag and whose check read with the same pattern, so the loss
would have passed it. Over the module's own sources it finds nothing to rewrap; it is run over no
engine. The README says it and AGENTS.md's scripts row names it and the sweep, new paths both. The
strict build is clean and the tests pass, 87 where there were 82.

The rewrapper's files mended in the commit carrying this line, as the review found it walking every
file under a folder, a submodule's, a fetched toolchain's and an ignored game's among them. The
files are those the repository holding each path tracks under it, as `git ls-files` lists them, of
the languages the sweep reads, and the count of comment lines past the width is taken by the
sweep's own rule before and after, so the lines rewrapped and those left add up to the sweep's
count. RewrapTests failed first on a made repository holding a tracked file, an ignored one, an
untracked one and a repository of its own under it, which git tracks as a commit, the old walk
rewriting all four; the tracked file alone is rewritten and counted, and the sweep's count over the
same repository is the same. Dry, over the three at their heads (python3 -I scripts/rewrap.py),
nothing written:

| repository | rewrapped | left | the sweep's count |
|---|---|---|---|
| 3DEngine | 5,463 in 1,171 files | 83 | 5,546 |
| BevyCSharp | 4,877 in 1,117 files | 12 | 4,889 |
| Voxel-Sandbox | 1,509 in 187 files | 5 | 1,514 |

| repository | top folder | files | lines |
|---|---|---|---|
| 3DEngine | `3DEngine` | 688 | 4,438 |
| 3DEngine | `3DEngine.Tests` | 195 | 490 |
| 3DEngine | `3DEngine.Examples` | 231 | 348 |
| 3DEngine | `games` | 35 | 100 |
| 3DEngine | `.github/workflows` | 2 | 29 |
| 3DEngine | `build` | 10 | 16 |
| 3DEngine | the rest, the Generator, the Cli, CodeFixes, the props and a template | 10 | 42 |
| BevyCSharp | `BevyCSharp` | 552 | 3,261 |
| BevyCSharp | `BevyCSharp.Tests` | 221 | 655 |
| BevyCSharp | `BevyCSharp.Examples` | 151 | 280 |
| BevyCSharp | `BevyCSharp.Editor` | 64 | 254 |
| BevyCSharp | `native` | 68 | 146 |
| BevyCSharp | `games` | 26 | 120 |
| BevyCSharp | `BevyCSharp.Generator` | 12 | 109 |
| BevyCSharp | the rest, Scripting, the workflows, the Cli, `build`, CodeFixes and the Player | 23 | 52 |
| Voxel-Sandbox | `VoxelSandbox` | 124 | 1,315 |
| Voxel-Sandbox | `VoxelSandbox.Tests` | 59 | 164 |
| Voxel-Sandbox | three build scripts and the workflow | 4 | 30 |

No tracked tree holds a vendored source; every folder is the repository's own, the engines' games
and BevyCSharp's Rust under `native` among them, and no tracked path lies under a folder named
vendor, third-party, external or deps. 3DEngine's ignore file leaves out `/Voxel-Sandbox`, which the
old walk read as 3DEngine's as well. The strict build is clean and the tests pass, 88 where there
were 87.

The day's sweep in the commit carrying this line. `scripts/daily.py` sweeps the repositories given
by `sweep.py`'s own reading, made a function, `read_tree`, that both call, and writes one Markdown
report to the path given, a row a repository with its failures, its warnings, their change and the
time taken, then for each its count a rule with its change, its counts a top folder and the ten
files a rule holds most of with the rest counted. Given the report before, it reads that report's
counts from the JSON an HTML comment at its end carries, so the Markdown is free to change its form,
and a rule no longer broken stands at 0 with its fall; a file that carries no counts is refused by
name. It reads the repositories and writes the report alone, and a name is put out of sight in a
place and a path. DailyTests reads a report over a faulty and a clean repository, every count by
rule and by folder, the worst files and the repositories' status unchanged, a second report's
change from the first with a rule no longer broken, twelve files of which ten are listed and two
counted, a made-up name counted and written nowhere, and a file that is no report refused with no
report written. Over 3DEngine, BevyCSharp, Voxel-Sandbox and this repository at their heads it
wrote its report in 7.7 seconds, the second with the first as its previous the same and every change
0. The comment in DevlogTests that named a runner, the one warning the module's own tree gave, says
a workflow. The strict build is clean and the tests pass, 93 where there were 88.

Verdict 2's mend in the commit carrying this line, with `6b711ed` before it. The rewrapper asked
git for the top of the repository holding a path and listed the files under the path made relative
to that top, and git gives the top by the path a symbolic link leads to, as macOS reaches its
temporary folder, `/var` by `/private/var`, so the path given through the link named nothing and no
file was read. It lists the files from the path's own folder with their paths from the top, so no
path given is made relative to one read another way. RewrapTests failed first on Linux on a made
repository given by a symbolic link to it, reading no file where it holds one, and rewraps it; the
test is skipped on Windows, which makes a link only in developer mode or for an administrator. A
file given through a link is read as well, and the dry run over 3DEngine at its head gives 5,449
lines rewrapped and 83 left, the sweep's 5,532. The strict build is clean and the tests pass, 94
where there were 93.

A proposal, not built, for the modules' four agnostic checks as one piece. The four are the
references (ReferenceTests, in Levels, Sky, Vegetation and Materials), the engines' names in the
public surface and its documentation (Levels' EngineNameTests at `80bbc12`), every public type named
in the README's binding section or a table, and the README's binding block standing word for word in
the test's host (both in Sky's BindingTests at `d4b3b0a`). The four copies of ReferenceTests differ
from Tools' in their names alone today (`diff` of each against `Tools.Tests/ReferenceTests.cs`),
since the copies are hours old; Tools' own changed once since the first, its Library and the
comment over it at `9f6e0c6`.

The checks become plain functions of the library, as ShellForms and DocumentCopies are, returning
what they find, with no xunit in them: `ModuleChecks.Refused(library, depsFile)`,
`ModuleChecks.EngineNames(library, documentationFile)`, `ModuleChecks.Unnamed(library, readme)`
and `ModuleChecks.BindingBlock(readme, hostSource)`, one list of the engines' names and the
refused names shared by the first two. A module's tests keep their own host, which is the module's,
and hold four facts of a line or two that assert each list empty.

| | A, Tools as the module's submodule, referenced from its tests | B, files copied and compared |
|---|---|---|
| what the module holds | `modules/Tools` in `.gitmodules`, one ProjectReference in its test project, four facts of 15 lines | the four tests' files, 250 lines, each opening with the Tools commit it was copied at, its names in one file of its own |
| its workflow | `submodules: true` on the checkout, and 2 seconds more for the library's build | nothing |
| a change to a check | reaches a module when its pointer moves, one commit there, as an engine takes it (N 7.1) | reaches a module when its files are copied again |
| drift | none, the build compiles the check a pointer names | read by a Tools script over the modules' checkouts in the day's sweep, each copy against Tools at its commit and at the head |
| a stranger | runs the tests after `git clone --recurse-submodules`; the library ships with no Tools in it | runs the tests as cloned |
| an engine taking the module | builds the module's library alone, and clones with no recursion, so Tools stands once in it | nothing |
| the copies of STYLE.md and COMMITS.md | a fifth fact, `DocumentCopies` against the sources at the pointer, in the module's own suite | read by the Tools script beside the tests' files |

The proposal is A. Rules held here change often, commitcheck.py's in four commits after the one that
made it, the same day (git log --oneline -- scripts/commitcheck.py), a copy costs a commit in seven
repositories each time, and a pointer moves a change in with the same one commit while the build
proves the module and the check agree; a stranger pays one flag on the clone, and the module's
library still ships with nothing of Tools'. The cost A carries is the nested submodule in an
engine's tree, which an engine's clone leaves unfetched unless it recurses, and B is the fallback if
the review reads that cost as too high.
The review chose A, and ModuleChecks is in the commit carrying this line with the proposal above.
`ModuleChecks` holds the four checks as functions that return what they find, each with a form
that reads a built assembly and a form that reads what it is given: `Refused` over a library's
assembly references and the packages its dependency file records, its names `RefusedNames`, the
engines', the windows', the graphics APIs' and every assembly of the prefix this one carries, so a
library that takes Tools is refused; `EngineNames` over the public types and members and the
documentation of the public members alone, since a regular expression's generator documents the
private method it writes with its pattern, which the first run over this library found; `Unnamed`
over the public types the README names neither in its section Binding it in your engine nor in a
table; and `BindingBlock` between the section's first C# block and a host's lines between its two
markers. ModuleChecksTests plants a fault for each, a refused name and one passed, a package a
library brings through another, a library that takes Tools beside a test project that does, a
public name, a public member's summary and a private member's remark of which the last is passed,
a type named outside the section and one named only as the start of another, a block that differs
by a line, and it runs the README's block of four facts word for word over this library. It builds
a module's test project from the README's own items with no submodule beside it, and the build
stops before its restore with one error, the sentence naming `git submodule update --init
modules/Tools`, and no compiler's or restore's error. The README's section names the module's three
additions, the submodule, the test project's two items and the facts, with a table of this
library's types, and ReferenceTests' cases move into ModuleChecksTests. The strict build is clean
and the tests pass, 100 where there were 94.

The allowed modules in the commit carrying this line, while the owner answers whether one module may
take another. `ModuleChecks.Refused` takes a list of modules a library may take, each by its whole
assembly name and none where none is given, so the answer costs a module one argument. A test
refuses four of five references with none allowed and three with one module allowed, a module whose
name begins with the allowed one's and another module still refused. The parameter's documentation
named a module by its prefix as its example, which the engines' names check found in this library's
own documentation, so the example stands in the README alone. The strict build is clean and the
tests pass, 101 where there were 100.

The day's report's breakdown in the commit carrying this line. Each repository's section gives a
sentence of its failures, how many the rewrap would clear and how many are for hands, and a table by
rule and top folder, largest first, with the rewrap's clearing beside each count, read by
`rewrap.py`'s own function over every file the width rule holds, without writing, a refused file
clearing nothing. DailyTests failed first on a made repository of three lines of prose the rewrap
fills, a comment's indented code it leaves and a dash it does not mend, and reads 4 of the width
rule in `scripts` with 3 cleared and 1 for hands, the dash's 1 for hands, the sentence of 5
failures, and the repository's status unchanged. At the heads, 3DEngine's 5,644 failures are 5,449
the rewrap would clear and 195 for hands, BevyCSharp's 4,967 are 4,821 and 146, and Voxel-Sandbox's
1,542 are 1,413 and 129. The rewrap's count of a file read each line by the width rule twice, 6.0 of
the 8.4 seconds its reading took over 3DEngine (python3 -I -m cProfile), and reads a line within the
width outside MSBuild and XML no more, where no comment runs on over lines, which takes it to 0.8
with the counts the same. The four repositories' report took 17.5 seconds at a load of 13 against
7.7 at rest without the breakdown, the sweep of 3DEngine alone 4.2 at that load against 1.7 at
rest. The strict build is clean and the tests pass, 102 where there were 101.

The rewrapper's tables in the commit carrying this line, found while sorting the hand work. A
comment's line opening with `|` is a table's row, which stood in a paragraph as prose, so a row
beside a long line was filled into it and the table lost, the words the same and the check passing.
A row stands as it is and ends a paragraph. RewrapTests failed first on a long line over a table of
three rows, one past the width, the rows filled into the paragraph; the paragraph is filled, the
rows keep their lines and the long row is counted as left. The repositories' comments hold such
rows, 6 in BevyCSharp within the width and 1 in Voxel-Sandbox past it (grep of the tracked files'
comment lines opening with a bar). The
strict build is clean and the tests pass, 103 where there were 102.

The width rule's C-family comments in the commit carrying this line, found while sorting the hand
work. A line opening with `*` was read as a block comment's inner line wherever it stood, so a line
continuing a product, `* Matrix4x4.CreateTranslation(...)`, failed as a comment, and so did a line
of data led by its number in a block comment that closes, `/* 0 */ [TL,HH,...]`. The rule follows
an open block comment over lines as it follows an XML comment, a `*` line being an inner line only
inside one, and a line opening with a closed block comment is code where code follows the close.
SweepTests failed first on a C# file holding both beside an open block comment, its inner line and
a closed block comment alone on its line, counting four where three are comments. The rewrapper's
count skips a line within the width only where it opens or closes no comment, so the counts still
agree: at the heads 3DEngine's comment lines past the width are 5,519, 5,449 rewrapped and 70 left,
BevyCSharp's 4,832, 4,821 and 11, and Voxel-Sandbox's 1,413, 1,412 and 1 (python3 -I
scripts/sweep.py and scripts/rewrap.py over each). The strict build is clean and the tests pass, 104
where there were 103.

The module's checks revised in the commit carrying this line, as Levels' trial at `46a7f8c` read
them. The README's block is four facts, one a check, so a module sees every fault at once where one
fact stopped at its first assert, and each calls a form of its check that returns null or a sentence
naming what it found, the rule and what to do, `ReferenceFault`, `EngineNameFault` and
`UnnamedFault`, where an empty assert printed a bare list; `BindingBlock`'s sentences say what to do
as well. The README states the host's two markers and that an engine taking a module builds its
library alone and clones with no recursion into the module's own `modules/Tools`.
`ShellForms.Scripts` leaves out the submodules `.gitmodules` names and any folder holding a `.git`
of its own, since a module's test read Tools' own `build/version.sh` as the module's script;
ShellFormsTests failed first on a checkout with both, reading three scripts where one is its own.
The sweep, the rewrapper and the commit's check read a repository's tracked files, in which git
lists a submodule as the commit it points at and none of its files, so a test of each over a
repository holding a submodule with a long comment and a dash reads none of them; the rewrapper's
was in already. ModuleChecksTests hosts the block of four facts and reads each fault's sentence on a
planted fault and null on none. The strict build is clean and the tests pass, 111 where there were
104.
The day's report's hand work in the commit carrying this line. Each repository's section gives a
table of what a hand does with each failure the rewrap leaves, with its count and three places in
the file as it stands: break an indented line of a comment, shorten a table's row, break a
documentation tag that stands alone, shorten a word or a tag longer than the line holds, break a
line of a block comment, rewrap by hand a file the rewrap refuses, and mend each rule other than
the width. A place is the original line a left line stands on or held its long word, each named
once while another is left. DailyTests failed first on a made repository holding one of five
kinds, the five rows read with their counts and first places. At the heads, from the report over
the three (python3 -I scripts/daily.py):

| repository | for hands | the pictures' ground | an indented comment line | British spelling | "word" | a tag alone | a long word | a table's row | em dash |
|---|---|---|---|---|---|---|---|---|---|
| 3DEngine | 182 | 83 | 69 | 22 | 7 | 0 | 1 | 0 | 0 |
| BevyCSharp | 145 | 127 | 6 | 3 | 3 | 4 | 1 | 0 | 1 |
| Voxel-Sandbox | 125 | 124 | 0 | 0 | 0 | 0 | 0 | 1 | 0 |

3DEngine's 69 indented lines are 58 in its library, 12 of them in `Graphics/GraphicsDevice.cs`, 6
in its generator, 3 in its examples and 2 in `build` (the report's hand work read by folder); the
pictures are the ground the table of 2026-11-18 read. The strict build is clean and the tests
pass, 112 where there were 111.

The rewrap's proof in the commit carrying this line. `scripts/rewrapcheck.py` copies a
repository's tracked files of the languages the rewrap reads into a folder of their own under the
work folder given, makes it a repository, runs `rewrap.py --write` over the copy alone, and holds a
file where its code, every line but its comment lines as the script reads them by a rule apart from
the rewrap's, is byte-equal before and after, its comments' words are the same once their blanks
are joined, and a second run changes nothing. Run over the three at their heads, a copy each in
the session's scratch folder (python3 -I scripts/rewrapcheck.py <work> ..., 6.6 seconds, 59 MB),
every file holds, the repositories' `git status --porcelain` the same before and after:

| repository | files copied | rewrapped | lines rewrapped | lines more | left | the second run |
|---|---|---|---|---|---|---|
| 3DEngine | 1,820 | 1,170 | 5,449 | 3,496 | 70 | nothing changed |
| BevyCSharp | 2,237 | 1,114 | 4,821 | 2,783 | 11 | nothing changed |
| Voxel-Sandbox | 221 | 186 | 1,412 | 869 | 1 | nothing changed |

RewrapCheckTests reads a made repository rewrapped in its copy alone with the second run changing
nothing, a copy of the scripts whose rewrap changes a line of code with its own check of the code
switched off, which the proof finds by its own rule, and a work folder inside the repository
refused. The rewrap's run over the repositories themselves stays the owner's word. The strict build
is clean and the tests pass, 115 where there were 112.

The commit's check's exit codes in the commit carrying this line. It exited with 1 where any line is
a FAIL and with 0 otherwise from `2a8d768` on, which its tests read on every planted FAIL and on a
commit of warnings alone, so that test could not fail first; two more read the FAIL paths no test
held, a branch under `claude/` and a range whose earlier commit fails, and passed at once. A check
that could not run, a revision git does not know or a pattern that is no regular expression, exited
with 1 as well, so a script could not tell it from a failing commit; it writes its reason to the
error output and exits with 2, which the test of both causes failed first on. The README says 0, 1
and 2 in both its sections. The strict build is clean and the tests pass, 118 where there were 115.

Levels' findings on the module's checks at `3386a62` in the commit carrying this line. xunit's
`Assert.Null` prints the first fifty characters of a string it is given, so a fault's sentence
showed what it found and never the rule or what to do; the README's four facts give the sentence
to `Assert.True(fault is null, fault)`, which prints it whole. `EngineNameFault` said "name an
engine" of one member and says "names" of one and "name" of more. ModuleChecksTests failed first
on a module made as the README lays it out, this repository's library copied as its submodule, its
test project the README's items and its test the README's four facts, whose library references a
binding named for SDL3, documents a type as an engine draws it, keeps a type out of its README and
holds a block no host holds: `dotnet test` over it failed all four with each sentence cut and the
singular wrong, and fails them with each sentence whole, in 3 seconds. The strict build is clean
and the tests pass, 119 where there were 118.

The devlog in the commit carrying this line. The README opens with a link to DEVLOG.md, and its
devlog section says that a module keeps its own, generated again in every commit that closes an
item of its review, by the script of the Tools it takes as its submodule, run from its root with its
name, `python3 -I modules/Tools/scripts/devlog.py . Levels`. Run with no repository, devlog.py ended
in an IndexError; it prints how it is run and exits with 1, which DevlogTests failed first on.
DEVLOG.md is generated again, the sheet composer's picture on 2026-11-18 at its head; the items this
session closes are the review's to mark done, and the devlog takes each on the day of its commit
when they are. The strict build is clean and the tests pass, 120 where there were 119.

The modules' cheatsheet in the commit carrying this line, the engines' form made once, with this
repository's own page. `Cheatsheet.Write` writes a module's CHEATSHEET.md from its built library and
documentation file, read by reflection where 3DEngine's writer reads the engine's source, every
public type in the order the README first names it in code, the rest after by name, with its public
members a line each in the order the build declares them: the declaration without its modifiers, its
nullable references, defaults, `params` and `init` as the source writes them, and the first sentence
of its summary with a reference written as the name it refers to, at the engines' column of 57. The
list is the C# block under `<!-- written by Cheatsheet.Write from the built library -->`, the text
around it kept by hand, and an empty page is given a first one. `ModuleChecks.CheatsheetFault` is
null where the page holds what the writer writes, names the first line that differs and what to do
where it does not, and with `CHEATSHEET_WRITE=1` writes the page, which is how this repository's own
CHEATSHEET.md was written, its six types and 39 members. CheatsheetTests reads this library's page
through the check, every line with its summary and no modifier, the declarations of a record, a
property set at its making, a `params` and a nullable default, the order a README gives, the hand's
text kept and a second writing the same, a page that differs or lost its marker named, and a
summary's first sentence with its references as names. The README's section says it, with the fact a
module adds beside its four once the review orders it; AGENTS.md's table of documents lacks a row
for the page, which waits on the owner's word (N 7.4). The strict build is clean and the tests pass,
127 where there were 120.

The cheatsheet's record constructors in the commit carrying this line, with AGENTS.md's row for
CHEATSHEET.md, which the owner's word on the modules' cheatsheets allows. A record's constructor
is documented by its type's summary, so its line repeated the type's word for word; a member whose
summary is its type's carries no comment, and the type's line says it once. CheatsheetTests failed
first on ShellForm's and ShellFinding's constructors, and this repository's page is written again
with them bare. The strict build is clean and the tests pass, 128 where there were 127.

The modules' findings on the cheatsheet in the commit carrying this line. A member documented by
`<inheritdoc/>` read as having no summary, as Modeling found of five; the writer takes the summary
its `cref` names, or else the one of the member it overrides or the interface's it implements. A
type's name carries its arity, `SceneFieldPlan`2`, which a README never writes, so `Unnamed` and
`UnnamedFault` take it off and find the plain name or the name with its parameters. The tests'
project writes its documentation file, which the tests read of sample types the compiler documents,
an interface's property and method taken by `<inheritdoc/>`, a property by `<inheritdoc cref>`, an
override of an abstract method, a positional record struct and a generic class; the inherited
summaries and the arity failed first. The positional record passed as it stands: the compiler
documents its properties from its `<param>` tags and its constructor from its type's summary, and
the constructor's line has carried no comment since `3720852`, so Landscape's four records needed
no constructors written out. The README says what a positional record needs, a summary on its type
and a `<param>` for each parameter. The strict build is clean and the tests pass, 132 where there
were 128.

The owner's rule that a module names no engine, its first part in the commit carrying this line.
`ModuleChecks.EngineNamesInText` reads every tracked text file of a module's checkout but its
`modules/`, `.github/REVIEW.md` and `.github/NORM.md` for a line that names an engine, 3DEngine,
BevyCSharp or Bevy or E3D as a word, outside the form `3DEngine.<Name>` its assembly and package
take, and `EngineNameInTextFault` names the first with what it names and how many more do.
ModuleChecksTests failed first on a made repository whose README and a comment name an engine,
beside the assembly's name, the review's, the norm's and a submodule's files and an untracked one,
finding the two lines alone, and null when they go. The README's section gives the fact a module
adds; this repository's own tests run it over made repositories alone, since its scripts and reports
name the repositories they read. Read over the seven modules at their heads, Levels and Sky hold no
such line, Landscape 2, Vegetation 8, Materials 9, Modeling 10 and RadianceCascades 744, of which
741 are in its `.github/KERNELS.md`; each of Landscape's, Modeling's, RadianceCascades' and
Vegetation's AGENTS.md has one, which waits on the owner's word (N 7.4). Tools' library names an
engine in two lines, ModuleChecks' refused names, which must hold the engines' assemblies to refuse
them, and the pattern the engines' names are found by, and neither can go; its project's
`3DEngine.Tools` is the form the rule keeps. The strict build is clean and the tests pass, 133 where
there were 132.

The owner's rule's second part in the commit carrying this line. In a repository under `Modules`
other than Tools, `commitcheck.py` fails an added line of a text file that names an engine outside
the form `3DEngine.<Name>`, its own `modules/`, `.github/REVIEW.md` and `.github/NORM.md` apart; a
repository is a module where its checkout's folder stands in a folder named Modules.
CommitCheckTests failed first on one commit made three times, under `Modules/Sample`,
`Modules/Tools` and `Other/Sample`, its README naming an engine beside an assembly's name and a
review's line, the module's failing on the README's line alone and the other two passing. The README
says it. The strict build is clean and the tests pass, 134 where there were 133.

Other products in a module's text in the commit carrying this line, as a warning. A line naming
Unity, HDRP, Unreal or Godot in a module's tracked text, a license's file and a line citing a source
by an address or a year apart, is listed by `ModuleChecks.ProductNamesInText`, a warning a reviewer
reads and no fact, and `commitcheck.py` warns of each such line a module's commit adds, once a line
where a comment's pattern warns of it already. A test of each failed first on a made module whose
README names a product, cites one by its year and one by an address, beside a license's file and a
comment naming one, the README's first line and the comment alone read. The seven modules at their
heads name none (grep of their tracked files). The README says it. The strict build is clean and the
tests pass, 136 where there were 134.

The README's list of what a module carries in the commit carrying this line, under its section
Binding it in your engine: the submodule and the test project's items of `bc59c55`, then each of the
six facts with the check it calls, the commit that gave it its form and the commits that changed
what it reads after, `7d56e0f` for the four, `17860cb` for the cheatsheet with `3720852` and
`183e308` after, and `b817e9b` for the text, so a module pinned before a fact's commit knows it owes
it; the products' list of `d1b04e5` stands below as a list and no fact. The hashes are read as
ancestors of the head (git merge-base --is-ancestor). The strict build is clean and the 136 tests
pass.

The README's top row in the commit carrying this line, since two modules wrote a middle dot between
their devlog's and cheatsheet's links and were stopped at the push. "What a module carries" says
the row's links are separated by a space, with the row as its example, and this README's row links
the cheatsheet beside the devlog. The 136 tests pass.

The README's link row by the owner's word in the commit carrying this line. A module's README opens
with its link row as its first line, above its title, `[Devlog](...)` and `[Cheatsheet](...)` on
its repository separated by a space, then a blank line and `# <Name>`, as this README opens where
its row stood below the title. "What a module carries" gives the form with the row as its example,
where the note on the row stood, and the README's block of facts holds a fifth,
`The_Readme_Opens_With_Its_Link_Row`, calling `ModuleChecks.LinkRowFault`, whose module is its
assembly's name after the prefix. ModuleChecksTests failed first on README openings with the title
first, a middle dot between the links, the devlog's link alone, no blank line and another title,
each named, and the row with either line end passing; the block's own check found the README's fifth
fact lacking the blank line the host has. The seven modules' READMEs open with the row at their
heads (sed of their first three lines). The strict build is clean and the tests pass, 138 where
there were 136.

The commit's hook in the commit carrying this line, within the review's limits. `scripts/hooks.py
install` writes a post-commit hook into the repository the working folder lies in, which runs
`commitcheck.py` on the commit just made and prints its lines, and `remove` takes it out; there is
no hook before a push. The hook exits with 0 whatever the check finds and prints nothing where
Python or the check's script is missing, the names coming from `FORBIDDEN_NAMES` in the committing
process's environment, which the hook neither reads nor writes. A repository the working folder
does not lie in is refused, so a session installs it in its own alone, and a post-commit hook of
another's is left as it is on install and on remove. The check is named from the repository's top
where it lies inside it, as a module's `modules/Tools` does. HooksTests reads a made repository
whose bad commit is made and prints its FAIL and whose good one prints its line, a second install
changing nothing and a removal taking the hook out, a repository elsewhere and another's hook
refused and left, and a hook whose check is gone or whose path holds no Python printing nothing
with the commit made. The hook is not installed in this repository, its session's tools refusing
the write to `.git/hooks`, which stays the owner's to allow. The README's section says how a module
installs it, the facts' table names `0b0b902` for the link row, and AGENTS.md's scripts row names
hooks.py, a new path. The strict build is clean and the tests pass, 143 where there were 138.

The other systems' faults made on Linux in the commit carrying this line, the second of the two
proposed. `build/variants.sh` runs the tests again with the temporary folder reached through a
symbolic link, as macOS reaches `/var` by `/private/var`, and every program given the locale
en_US.ISO-8859-1, whose pipe is single-byte as Windows gives cp1252, and says so and exits with 2
where the locale is missing; the workflow's Linux job installs the locale and runs it after the
tests. A script that ends in a traceback fails its test whatever exit code the test expects, since
under the single-byte locale the sweep of before `7dbd4b3` crashed on its dash and a test expecting
1 read the crash's 1 as its answer. Read against the scripts of before each mend: the rewrapper of
before `b615a94` fails 7 of its tests under the variant where it fails 1 plain, and the sweep and
commit's check of before `7dbd4b3` fail 8 of the sweep's tests where they fail 7 plain, the newer
tests asking of the old scripts what they had not yet; the head passes both runs. VariantTests,
skipped outside the variant with its reason, reads in it that the temporary folder is reached
through a link and that a pipe is single-byte, so a machine that gave neither would not pass the
second run having proved nothing. The README says it. The strict build is clean and the tests pass,
143 and one skipped plain and 144 in the variant, in 4.7 seconds.

The locale's step in the commit carrying this line, as `2c021c6`'s run read it (44365758695, green
on the three systems). Ubuntu 24.04 refused `locale-gen en_US.ISO-8859-1` as no supported locale and
made it by the name `en_US`, the fallback, the variant's run then passing 144 with its own test run;
the step runs `sudo locale-gen en_US` alone, and the script's message and the README name the name
Ubuntu takes. The tests pass plain and in the variant.

The cheatsheet as one word by the owner's word in the commit carrying this line. `LinkRowFault`'s
row is `[Devlog](...) [Cheatsheet](...)`, the writer's first page is titled `# 3DEngine.<Name>
cheatsheet`, and this repository's own lines that wrote it as two words, 18 of them with its
README's row, its sections, its page's title, two comments and this file's Replies, are written as
one, git grep -n -i for the two words finding none. `line_faults` fails the two words on any line of
any repository, code and prose alike, so the sweep reads every line for them and the commit's check
fails an added one. The tests failed first, the link row's form, the first page's title, the sweep
over a README and a C# string, and a commit's added string, the planted words joined from parts so
the tests' own files hold them on no line. The README says it. The modules' rows and titles fail the
link row's fact until they are written as one word. The strict build is clean and the tests pass,
146 and one skipped plain and 147 in the variant.

The status for the hourly watch in the commit carrying this line. `scripts/status.py` prints a line
a repository and writes nothing: its local head, its remote head read by `git ls-remote`, which
fetches nothing, and how many commits it is ahead, or a question mark where the remote's head is
not fetched; its last build run's head, status and conclusion through gh, behind one function the
tests replace by recorded runs in STATUS_ANSWERS; its `.github/REVIEW.md`'s size where it passes 90
KB; whether DEVLOG.md was last committed before its newest picture under `.github/assets/readings`
or `.github/devlog`; its pin of Tools against Tools' head, the checkout it stands in or the one
`--tools` names; and whether its README opens with the link row. With no repository given it reads
3DEngine, BevyCSharp and Voxel-Sandbox beside Modules and every folder of Modules, the repositories
read at once and printed in order, the eleven in 2.5 seconds where one after another took 14. The
exit code is 0, or 2 where git or gh could not be reached, its line saying which. StatusTests reads
made repositories whose remote's address in GitHub's form git sends to a bare repository beside
them: a line with its heads, one commit ahead, its run, a review of 95 KB, a picture after the
devlog, a pin one behind a made Tools and the row; a README without the row, no run and a pin at
the head; and a folder outside git and a recorded failure of gh each giving 2. Over the eleven at
their heads it reads every module's row missing, the two-word form `75d77f2` changed standing in
them until they reword it, 3DEngine's review at 98 KB and Voxel-Sandbox's at 91, the devlog behind
its newest picture in BevyCSharp, Voxel-Sandbox and RadianceCascades, and Modeling two commits
ahead of its remote. The README says it and AGENTS.md's scripts row names it. The strict build is
clean and the tests pass, 149 and one skipped plain and 150 in the variant.
