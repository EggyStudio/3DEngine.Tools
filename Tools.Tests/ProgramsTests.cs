using Xunit;

namespace Tools.Tests;

public class ProgramsTests
{
    // A copy built beside the tests stays as it was until the next build, so a test run after an
    // edit with no build read the old script and could pass what the edit should fail.
    [Fact]
    public void A_Test_Runs_Each_Script_From_The_Checkout_And_No_Copy_Stands_Beside_The_Tests()
    {
        Assert.Equal(Path.Combine(Programs.Root(), "scripts", "sweep.py"), Programs.ScriptPath("sweep.py"));
        Assert.False(Directory.Exists(Path.Combine(AppContext.BaseDirectory, "scripts")), "the build copies the scripts beside the tests");
    }
}
