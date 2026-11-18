# Tools

Tools is a library both engines, 3DEngine and BevyCSharp, take as a git submodule, for what the
repositories otherwise copy by hand, the devlog's generator, the composer of reading sheets, the run
reader, the texture recipes, and the text STYLE.md, COMMITS.md and NORM.md share. It references no
engine, and each engine implements the interfaces it declares where it binds the module.

## Taking it into an engine

An engine adds the repository as a git submodule under `modules/` and references its project, here
from a project one folder below the engine's root.

```bash
git submodule add https://github.com/EggyStudio/Tools.git modules/Tools
```

```xml
<ProjectReference Include="..\modules\Tools\Tools\Tools.csproj" />
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
python3 -I scripts/devlog.py <repo path> <Name> [sent-pictures.json] [--fixes <fixes.json>]
```

`--fixes` names a JSON file kept outside the repositories, an object whose `pairs` are a pattern and
its replacement each, applied in turn to every caption and title, whose `patterns` give by a
repository's name the pattern its STYLE.md adds to the check, and whose `undated` is the day a sent
picture with no date is put on. With none given it rewrites nothing, adds no pattern and stops at a
sent picture with no date, naming it.

It prints the days, pictures and items it wrote, and any line that breaks STYLE.md's mechanical
rules.

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

A FAIL line stops a push and makes the exit code 1, a WARN line is for the reviewer to read, and a
note says what to look at. The names no commit may carry are a regular expression in the
`FORBIDDEN_NAMES` environment variable and in no file, and where it is not set the check says so.

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
