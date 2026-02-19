using ControlCenter.Contracts;

namespace ControlCenter.Application.Abstractions;

public interface IGatewayApiClient
{
    Task<GatewayStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentSummaryDto>> GetAgentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectSummaryDto>> GetProjectsAsync(CancellationToken cancellationToken = default);
}
