using ControlCenter.Application.Abstractions;
using ControlCenter.Contracts;

namespace ControlCenter.Application.Services;

public sealed class SkillsService
{
    private readonly IGatewayApiClient _gatewayApiClient;

    public SkillsService(IGatewayApiClient gatewayApiClient)
    {
        _gatewayApiClient = gatewayApiClient;
    }

    public async Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _gatewayApiClient.GetSkillsAsync(cancellationToken);
        }
        catch (GatewayApiCompatibilityException)
        {
            return [];
        }
    }
}