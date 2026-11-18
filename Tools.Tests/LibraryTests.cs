using System.Reflection;
using Xunit;

namespace Tools.Tests;

public class LibraryTests
{
    [Fact]
    public void The_Library_Loads()
    {
        // By the assembly's name, 3DEngine and the module's, where the namespace is the module's
        // alone, so the test reads that the project reference puts the assembly of that name
        // beside the tests and that it holds the library's types.
        var library = Assembly.Load("3DEngine.Tools");
        Assert.Equal("3DEngine.Tools", library.GetName().Name);
        Assert.Same(library, typeof(ShellForms).Assembly);
    }
}
