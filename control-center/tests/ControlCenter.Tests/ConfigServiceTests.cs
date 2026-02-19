using ControlCenter.Application.Abstractions;
using ControlCenter.Application.Services;
using ControlCenter.Contracts;

namespace ControlCenter.Tests;

public sealed class ConfigServiceTests
{
    [Fact]
    public void BuildDiff_ReturnsChangedKeys()
    {
        var service = new ConfigService(new StubGatewayApiClient());

        var diff = service.BuildDiff(
            [new ConfigEntryDto("gateway.baseUrl", "http://localhost", false, "file")],
            [new ConfigEntryDto("gateway.baseUrl", "https://gateway", false, "file")]);

        Assert.Single(diff);
        Assert.Contains("gateway.baseUrl", diff[0]);
    }

    [Fact]
    public void ValidateEditorPayload_RequiresJson()
    {
        var service = new ConfigService(new StubGatewayApiClient());
        Assert.True(service.ValidateEditorPayload("{\"ok\":true}"));
        Assert.False(service.ValidateEditorPayload("not-json"));
    }

    [Fact]
    public void Snapshot_EnablesRollback()
    {
        var service = new ConfigService(new StubGatewayApiClient());
        var initial = new[] { new ConfigEntryDto("k", "v1", false, "file") };
        var changed = new[] { new ConfigEntryDto("k", "v2", false, "file") };

        service.Snapshot(initial);
        var rolledBack = service.RollbackOrCurrent(changed);

        Assert.Equal("v1", rolledBack[0].Value);
    }

    private sealed class StubGatewayApiClient : IGatewayApiClient
    {
        public Task<GatewayStatusDto> GetStatusAsync(CancellationToken cancellationToken = default) => Task.FromResult(new GatewayStatusDto("1", "dev", DateTimeOffset.UtcNow));
        public Task<IReadOnlyList<AgentSummaryDto>> GetAgentsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AgentSummaryDto>>([]);
        public Task<IReadOnlyList<ProjectSummaryDto>> GetProjectsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ProjectSummaryDto>>([]);
        public Task<IReadOnlyList<TaskRunDto>> GetActiveRunsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TaskRunDto>>([]);
        public Task<UsageSummaryDto> GetUsageSummaryAsync(CancellationToken cancellationToken = default) => Task.FromResult(new UsageSummaryDto(0, 0, 0m));
        public Task<IReadOnlyList<CronJobDto>> GetCronJobsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CronJobDto>>([]);
        public Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SkillDto>>([]);
        public Task<IReadOnlyList<ConfigEntryDto>> GetConfigEntriesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ConfigEntryDto>>([]);
    }
}