using ControlCenter.Application.Abstractions;
using ControlCenter.Contracts;

namespace ControlCenter.Application.Services;

public sealed class CronService
{
    private readonly IGatewayApiClient _gatewayApiClient;

    public CronService(IGatewayApiClient gatewayApiClient)
    {
        _gatewayApiClient = gatewayApiClient;
    }

    public Task<IReadOnlyList<CronJobDto>> GetJobsAsync(CancellationToken cancellationToken = default)
        => _gatewayApiClient.GetCronJobsAsync(cancellationToken);

    public string BuildSafetyConfirmation(string action, string jobName)
        => $"Confirm {action} for cron job '{jobName}'";
}