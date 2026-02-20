using ControlCenter.Application.Abstractions;
using ControlCenter.Application.Services;
using ControlCenter.Contracts;

namespace ControlCenter.Tests;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_BuildsTilesAndNeedsAttention()
    {
        var now = DateTimeOffset.UtcNow;
        var apiClient = new StubGatewayApiClient(
            new GatewayStatusDto("1.2.0", "Development", now),
            [
                new AgentSummaryDto("a1", "Agent One", "online", now),
                new AgentSummaryDto("a2", "Agent Two", "offline", now.AddMinutes(-10))
            ],
            [
                new ProjectSummaryDto("p1", "Project Healthy", "main", "abc123", "ok"),
                new ProjectSummaryDto("p2", "Project Broken", "main", "def456", "degraded")
            ]);

        var cache = new InMemoryGatewayCache();
        var service = new DashboardService(apiClient, cache);

        var snapshot = await service.GetSnapshotAsync();

        Assert.Equal(4, snapshot.Tiles.Count);
        Assert.Equal(2, snapshot.NeedsAttention.Count);
        Assert.Contains(snapshot.NeedsAttention, item => item.Category == "agent" && item.Title == "Agent Two");
        Assert.Contains(snapshot.NeedsAttention, item => item.Category == "project" && item.Title == "Project Broken");
    }

    [Fact]
    public async Task GetSnapshotAsync_ProjectApiUnsupported_StillReturnsSnapshot()
    {
        var now = DateTimeOffset.UtcNow;
        var apiClient = new StubGatewayApiClient(
            new GatewayStatusDto("1.2.0", "Development", now),
            [new AgentSummaryDto("a1", "Agent One", "online", now)],
            [new ProjectSummaryDto("p1", "Project", "main", "abc123", "ok")],
            throwProjectsCompatibility: true);

        var cache = new InMemoryGatewayCache();
        var service = new DashboardService(apiClient, cache);

        var snapshot = await service.GetSnapshotAsync();

        Assert.NotEmpty(snapshot.Tiles);
        Assert.DoesNotContain(snapshot.NeedsAttention, item => item.Category == "project");
    }

    private sealed class StubGatewayApiClient : IGatewayApiClient
    {
        private readonly GatewayStatusDto _status;
        private readonly IReadOnlyList<AgentSummaryDto> _agents;
        private readonly IReadOnlyList<ProjectSummaryDto> _projects;
        private readonly bool _throwProjectsCompatibility;

        public StubGatewayApiClient(
            GatewayStatusDto status,
            IReadOnlyList<AgentSummaryDto> agents,
            IReadOnlyList<ProjectSummaryDto> projects,
            bool throwProjectsCompatibility = false)
        {
            _status = status;
            _agents = agents;
            _projects = projects;
            _throwProjectsCompatibility = throwProjectsCompatibility;
        }

        public Task<GatewayStatusDto> GetStatusAsync(CancellationToken cancellationToken = default) => Task.FromResult(_status);
        public Task<IReadOnlyList<AgentSummaryDto>> GetAgentsAsync(CancellationToken cancellationToken = default) => Task.FromResult(_agents);
        public Task<IReadOnlyList<ProjectSummaryDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
            => _throwProjectsCompatibility
                ? throw new GatewayApiCompatibilityException("projects unsupported")
                : Task.FromResult(_projects);
        public Task<IReadOnlyList<TaskRunDto>> GetActiveRunsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TaskRunDto>>([]);
        public Task<UsageSummaryDto> GetUsageSummaryAsync(CancellationToken cancellationToken = default) => Task.FromResult(new UsageSummaryDto(0, 0, 0m));
        public Task<IReadOnlyList<CronJobDto>> GetCronJobsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CronJobDto>>([]);
        public Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SkillDto>>([]);
        public Task<IReadOnlyList<ConfigEntryDto>> GetConfigEntriesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ConfigEntryDto>>([]);
    }

    private sealed class InMemoryGatewayCache : IGatewayCache
    {
        public GatewayStatusDto? Status { get; private set; }
        public IReadOnlyList<AgentSummaryDto> Agents { get; private set; } = [];
        public IReadOnlyList<ProjectSummaryDto> Projects { get; private set; } = [];

        public Task SaveStatusAsync(GatewayStatusDto status, CancellationToken cancellationToken = default)
        {
            Status = status;
            return Task.CompletedTask;
        }

        public Task SaveAgentsAsync(IReadOnlyList<AgentSummaryDto> agents, CancellationToken cancellationToken = default)
        {
            Agents = agents;
            return Task.CompletedTask;
        }

        public Task SaveProjectsAsync(IReadOnlyList<ProjectSummaryDto> projects, CancellationToken cancellationToken = default)
        {
            Projects = projects;
            return Task.CompletedTask;
        }

        public Task<GatewayStatusDto?> ReadStatusAsync(CancellationToken cancellationToken = default) => Task.FromResult(Status);
        public Task<IReadOnlyList<AgentSummaryDto>> ReadAgentsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Agents);
        public Task<IReadOnlyList<ProjectSummaryDto>> ReadProjectsAsync(CancellationToken cancellationToken = default) => Task.FromResult(Projects);
    }
}