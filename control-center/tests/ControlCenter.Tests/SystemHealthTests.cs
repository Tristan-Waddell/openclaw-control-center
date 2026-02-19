using ControlCenter.Domain;

namespace ControlCenter.Tests;

public sealed class SystemHealthTests
{
    [Fact]
    public void Constructor_RejectsEmptyStatus()
    {
        Assert.Throws<ArgumentException>(() => new SystemHealth(string.Empty));
    }
}
