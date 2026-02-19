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

    public Task<IReadOnlyList<TaskRunDto>> GetActiveRunsAsync(CancellationToken cancellationToken = default)
        => _gatewayApiClient.GetActiveRunsAsync(cancellationToken);

    public Task<UsageSummaryDto> GetUsageSummaryAsync(CancellationToken cancellationToken = default)
        => _gatewayApiClient.GetUsageSummaryAsync(cancellationToken);

    public Task<IReadOnlyList<RealtimeEventEnvelopeDto>> GetRecentRunsAsync(int limit = 25, CancellationToken cancellationToken = default)
        => _eventJournal.ReadRecentAsync(limit, cancellationToken);
}