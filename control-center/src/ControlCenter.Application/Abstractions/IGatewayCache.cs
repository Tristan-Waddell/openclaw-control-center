using ControlCenter.Contracts;

namespace ControlCenter.Application.Abstractions;

public interface IGatewayCache
{
    Task SaveStatusAsync(GatewayStatusDto status, CancellationToken cancellationToken = default);
    Task SaveAgentsAsync(IReadOnlyList<AgentSummaryDto> agents, CancellationToken cancellationToken = default);
    Task SaveProjectsAsync(IReadOnlyList<ProjectSummaryDto> projects, CancellationToken cancellationToken = default);

    Task<GatewayStatusDto?> ReadStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentSummaryDto>> ReadAgentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectSummaryDto>> ReadProjectsAsync(CancellationToken cancellationToken = default);
}
