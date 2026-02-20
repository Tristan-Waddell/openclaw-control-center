using ControlCenter.Application.Abstractions;
using ControlCenter.Contracts;

namespace ControlCenter.Application.Services;

public sealed class ProjectsService
{
    private readonly IGatewayApiClient _gatewayApiClient;

    public ProjectsService(IGatewayApiClient gatewayApiClient)
    {
        _gatewayApiClient = gatewayApiClient;
    }

    public async Task<IReadOnlyList<ProjectSummaryDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _gatewayApiClient.GetProjectsAsync(cancellationToken);
        }
        catch (GatewayApiCompatibilityException)
        {
            return [];
        }
    }
}