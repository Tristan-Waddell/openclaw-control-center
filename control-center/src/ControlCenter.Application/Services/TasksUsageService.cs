using ControlCenter.Application.Abstractions;
using ControlCenter.Contracts;

namespace ControlCenter.Application.Services;

public sealed class TasksUsageService
{
    private readonly IGatewayApiClient _gatewayApiClient;
    private readonly IEventJournal _eventJournal;

    public TasksUsageService(IGatewayApiClient gatewayApiClient, IEventJournal eventJournal)
    {
        _gatewayApiClient = gatewayApiClient;
        _eventJournal = eventJournal;
    }

    public async Task<IReadOnlyList<TaskRunDto>> GetActiveRunsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _gatewayApiClient.GetActiveRunsAsync(cancellationToken);
        }
        catch (GatewayApiCompatibilityException)
        {
            return [];
        }
    }

    public async Task<UsageSummaryDto> GetUsageSummaryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _gatewayApiClient.GetUsageSummaryAsync(cancellationToken);
        }
        catch (GatewayApiCompatibilityException)
        {
            return new UsageSummaryDto(0, 0, 0m);
        }
    }

    public Task<IReadOnlyList<RealtimeEventEnvelopeDto>> GetRecentRunsAsync(int limit = 25, CancellationToken cancellationToken = default)
        => _eventJournal.ReadRecentAsync(limit, cancellationToken);
}