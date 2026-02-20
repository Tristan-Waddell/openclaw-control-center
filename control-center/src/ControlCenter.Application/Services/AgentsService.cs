using ControlCenter.Application.Abstractions;
using ControlCenter.Contracts;

namespace ControlCenter.Application.Services;

public sealed class AgentsService
{
    private readonly IGatewayApiClient _gatewayApiClient;
    private readonly IGatewayCache _gatewayCache;

    public AgentsService(IGatewayApiClient gatewayApiClient, IGatewayCache gatewayCache)
    {
        _gatewayApiClient = gatewayApiClient;
        _gatewayCache = gatewayCache;
    }

    public async Task<IReadOnlyList<AgentSummaryDto>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var agents = await _gatewayApiClient.GetAgentsAsync(cancellationToken);
            await _gatewayCache.SaveAgentsAsync(agents, cancellationToken);
            return agents;
        }
        catch (GatewayApiCompatibilityException)
        {
            return [];
        }
    }
}