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

    public Task<IReadOnlyList<ProjectSummaryDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
        => _gatewayApiClient.GetProjectsAsync(cancellationToken);
}