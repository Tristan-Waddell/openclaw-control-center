using ControlCenter.Contracts;
using ControlCenter.Infrastructure.Persistence;

namespace ControlCenter.Tests;

public sealed class SqliteGatewayCacheTests
{
    [Fact]
    public async Task SaveAndRead_RoundTripsStatusAndProjects()
    {
        var path = Path.Combine(Path.GetTempPath(), $"control-center-{Guid.NewGuid():N}.db");
        var cs = $"Data Source={path};Pooling=False";
        var cache = new SqliteGatewayCache(cs);

        var status = new GatewayStatusDto("1.0.0", "Development", DateTimeOffset.UtcNow);
        var projects = new List<ProjectSummaryDto>
        {
            new("proj-1", "Control Center", "main", "abc123", "ok")
        };

        await cache.SaveStatusAsync(status);
        await cache.SaveProjectsAsync(projects);

        var loadedStatus = await cache.ReadStatusAsync();
        var loadedProjects = await cache.ReadProjectsAsync();

        Assert.NotNull(loadedStatus);
        Assert.Equal("1.0.0", loadedStatus!.Version);
        Assert.Single(loadedProjects);
        Assert.Equal("Control Center", loadedProjects[0].Name);

        File.Delete(path);
    }
}
