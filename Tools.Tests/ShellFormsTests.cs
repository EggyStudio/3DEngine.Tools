using Xunit;

namespace Tools.Tests;

public class ShellFormsTests
{
    [Fact]
    public void Every_Script_Of_The_Module_Uses_Only_What_Both_Systems_Tools_Read()
    {
        var root = Programs.Root();
        var scripts = ShellForms.Scripts(root);
        Assert.Contains("build/version.sh", scripts);

        var found = ShellForms.Find(root, scripts);

        Assert.True(found.Count == 0, "Scripts use what only GNU's tools or bash 4 read (NORM.md, N 6.6):\n  " + string.Join("\n  ", found));
    }

    // The cases of both engines' tables, which these forms were read from.
    [Theory]
    [InlineData("sed -i \"s/PACKED_VERSION/$version/\" template.json", "sed -i")]
    [InlineData("sed -Ei 's/a/b/' file", "sed -i")]
    [InlineData("floor=$(objdump -T \"$BUILT\" | grep -oP 'GLIBC_\\K[0-9]+')", "grep -P")]
    [InlineData("count=$(grep -oP '\\d+' file)", "grep -P")]
    [InlineData("readarray -t lines < file", "readarray")]
    [InlineData("mapfile -t lines < file", "readarray")]
    [InlineData("then=$(date -d yesterday +%s)", "date -d")]
    [InlineData("size=$(stat -c %s file)", "stat -c")]
    [InlineData("sha256sum package.nupkg", "sha256sum")]
    [InlineData("echo \"${profile,,}\"", "${x,,}")]
    [InlineData("declare -A seen=()", "declare -A")]
    [InlineData("local -rA kinds", "declare -A")]
    [InlineData("make |& tee build.log", "|&")]
    [InlineData("echo done &>> run.log", "&>>")]
    [InlineData("declare -a seen=()", null)]
    [InlineData("make 2>&1 | tee build.log", null)]
    [InlineData("echo done >> run.log 2>&1", null)]
    [InlineData("sed -e 's/a/b/' file > file.new && mv file.new file", null)]
    [InlineData("floor=$(objdump -T \"$BUILT\" | grep -oE 'GLIBC_[0-9]+' | sed 's/^GLIBC_//')", null)]
    [InlineData("now=$(date +%s)", null)]
    [InlineData("size=$(wc -c < file)", null)]
    [InlineData("echo \"${profile}\" \"${#features[@]}\"", null)]
    [InlineData("# sed -i is left out, which macOS reads otherwise", null)]
    [InlineData("make  # sed -i is left out here as well", null)]
    public void Each_Form_Is_Found_Where_A_Line_Has_It_And_Nowhere_Else(string line, string? form)
    {
        string[] expected = form is null ? [] : [form];
        Assert.Equal(expected, ShellForms.In(line).Select(found => found.Name));
    }

    [Fact]
    public void A_Script_Run_On_MacOS_Is_Followed_To_The_Scripts_It_Names_And_A_Linux_Ones_Are_Not()
    {
        var root = Directory.CreateTempSubdirectory("shell-").FullName;
        try
        {
            // A macOS job runs a.sh, which runs b.sh and the launcher, a matrix job runs c.sh, and
            // an Ubuntu job runs d.sh, which no other job names. A script a.sh names in a comment
            // and one it names that the checkout lacks are left out.
            Write(root, ".github/workflows/build.yml", """
                on: push
                jobs:
                  mac:
                    runs-on: macos-15
                    steps:
                      - run: ./build/a.sh
                  each:
                    runs-on: ${{ matrix.os }}
                    steps:
                      - run: bash build/c.sh
                  linux:
                    runs-on: ubuntu-24.04
                    steps:
                      - run: build/d.sh
                """);
            Write(root, "build/a.sh", "build/b.sh\n./cli open\n# build/e.sh\nbuild/missing.sh\n");
            Write(root, "build/b.sh", "sed -i 's/a/b/' file\n");
            Write(root, "build/c.sh", "echo c\n");
            Write(root, "build/d.sh", "readarray -t lines < file\n");
            Write(root, "build/e.sh", "echo e\n");
            Write(root, "cli", "#!/usr/bin/env bash\n");
            Write(root, "bin/Debug/f.sh", "echo f\n");
            Write(root, "build/tools/g.sh", "echo g\n");

            Assert.Equal(["build/a.sh", "build/b.sh", "build/c.sh", "cli"], ShellForms.RunElsewhere(root, "cli"));
            Assert.Equal(["build/a.sh", "build/b.sh", "build/c.sh", "build/d.sh", "build/e.sh"], ShellForms.Scripts(root, "build/tools"));

            var found = Assert.Single(ShellForms.Find(root, ShellForms.RunElsewhere(root, "cli")));
            Assert.Equal("build/b.sh:1 has sed -i, where sed writing a file beside it, moved over it reads on both", found.ToString());
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void A_Submodules_Scripts_Are_No_Scripts_Of_The_Checkout()
    {
        var root = Directory.CreateTempSubdirectory("shell-").FullName;
        try
        {
            // A module that takes this repository as a submodule, whose scripts are its own and
            // read in its own checkout, and a nested repository no .gitmodules names.
            Write(root, ".gitmodules", "[submodule \"modules/Tools\"]\n\tpath = modules/Tools\n\turl = https://example.com/Tools.git\n");
            Write(root, "build/a.sh", "echo a\n");
            Write(root, "modules/Tools/build/version.sh", "sed -i 's/a/b/' file\n");
            Write(root, "modules/Tools/.git", "gitdir: ../../.git/modules/Tools\n");
            Write(root, "vendor/Other/.git/HEAD", "ref: refs/heads/main\n");
            Write(root, "vendor/Other/run.sh", "readarray -t lines < file\n");

            Assert.Equal(["build/a.sh"], ShellForms.Scripts(root));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void Write(string root, string path, string text)
    {
        var file = Path.Combine(root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text);
    }
}
