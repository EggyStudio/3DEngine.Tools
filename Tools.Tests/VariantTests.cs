using Xunit;

namespace Tools.Tests;

public class VariantTests
{
    // The run of build/variants.sh holds only where both of its conditions hold, so a machine that
    // gave neither would pass the second run having proved nothing.
    [InVariantFact]
    public void The_Variant_Reaches_Its_Temporary_Folder_Through_A_Link_Gives_A_Single_Byte_Pipe_And_Git_Crlf()
    {
        Assert.Equal("true", Programs.Run("git", AppContext.BaseDirectory, "config", "--get", "core.autocrlf").Trim());
        var temp = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        var target = new DirectoryInfo(temp).ResolveLinkTarget(returnFinalTarget: true);
        var (_, encoding) = Programs.Execute(Programs.Python.Value!, AppContext.BaseDirectory, [], "-I", "-c", "import sys; print(sys.stdout.encoding)");

        Assert.True(target is not null, $"{temp} is reached through no link");
        Assert.Equal("iso8859-1", encoding.Trim());
    }
}
