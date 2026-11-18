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

None yet.

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
