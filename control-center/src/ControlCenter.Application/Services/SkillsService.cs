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

    public Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken cancellationToken = default)
        => _gatewayApiClient.GetSkillsAsync(cancellationToken);
}