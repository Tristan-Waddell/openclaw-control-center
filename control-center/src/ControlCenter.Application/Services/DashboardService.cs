using ControlCenter.Application.Abstractions;
using ControlCenter.Contracts;

namespace ControlCenter.Application.Services;

public sealed class DashboardService
{
    private static readonly TimeSpan StaleHeartbeatThreshold = TimeSpan.FromMinutes(5);

    private readonly IGatewayApiClient _gatewayApiClient;
    private readonly IGatewayCache _gatewayCache;

    public DashboardService(IGatewayApiClient gatewayApiClient, IGatewayCache gatewayCache)
    {
        _gatewayApiClient = gatewayApiClient;
        _gatewayCache = gatewayCache;
    }

    public async Task<DashboardSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var status = await _gatewayApiClient.GetStatusAsync(cancellationToken);
        var agents = await ReadOptionalAsync(() => _gatewayApiClient.GetAgentsAsync(cancellationToken));
        var projects = await ReadOptionalAsync(() => _gatewayApiClient.GetProjectsAsync(cancellationToken));

        await _gatewayCache.SaveStatusAsync(status, cancellationToken);
        await _gatewayCache.SaveAgentsAsync(agents, cancellationToken);
        await _gatewayCache.SaveProjectsAsync(projects, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var offlineAgents = agents.Count(agent => !IsHealthyAgent(agent, now));
        var unhealthyProjects = projects.Count(project => !string.Equals(project.Health, "ok", StringComparison.OrdinalIgnoreCase));

        var tiles = new List<HealthTileDto>
        {
            new("Gateway", status.Environment, "healthy", $"v{status.Version}"),
            new(
                "Agents",
                $"{agents.Count - offlineAgents}/{agents.Count}",
                offlineAgents == 0 ? "healthy" : "warning",
                offlineAgents == 0 ? "All active" : $"{offlineAgents} needs attention"),
            new(
                "Projects",
                projects.Count.ToString(),
                unhealthyProjects == 0 ? "healthy" : "warning",
                unhealthyProjects == 0 ? "All healthy" : $"{unhealthyProjects} unhealthy"),
            new("Cache", "SQLite", "healthy", "Synced")
        };

        var needsAttention = new List<NeedsAttentionItemDto>();

        foreach (var agent in agents.Where(agent => !IsHealthyAgent(agent, now)).OrderBy(agent => agent.Name))
        {
            needsAttention.Add(new NeedsAttentionItemDto(
                "agent",
                agent.Name,
                $"Status {agent.Status}; heartbeat {agent.LastHeartbeatUtc:u}",
                now));
        }

        foreach (var project in projects.Where(project => !string.Equals(project.Health, "ok", StringComparison.OrdinalIgnoreCase)).OrderBy(project => project.Name))
        {
            needsAttention.Add(new NeedsAttentionItemDto(
                "project",
                project.Name,
                $"Health is {project.Health} on {project.Branch}",
                now));
        }

        return new DashboardSnapshotDto(tiles, needsAttention, now);
    }

    private static bool IsHealthyAgent(AgentSummaryDto agent, DateTimeOffset now)
    {
        if (!string.Equals(agent.Status, "online", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(agent.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return now - agent.LastHeartbeatUtc <= StaleHeartbeatThreshold;
    }

    private static async Task<IReadOnlyList<T>> ReadOptionalAsync<T>(Func<Task<IReadOnlyList<T>>> fetch)
    {
        try
        {
            return await fetch();
        }
        catch (GatewayApiCompatibilityException)
        {
            return [];
        }
    }
}