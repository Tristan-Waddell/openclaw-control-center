using ControlCenter.Application.Abstractions;
using ControlCenter.Contracts;

namespace ControlCenter.Application.Services;

public sealed class RealtimeSyncService
{
    private readonly IRealtimeClient _realtimeClient;
    private readonly IEventJournal _eventJournal;

    public RealtimeSyncService(IRealtimeClient realtimeClient, IEventJournal eventJournal)
    {
        _realtimeClient = realtimeClient;
        _eventJournal = eventJournal;
    }

    public async Task<int> RunOnceAsync(
        IReadOnlyList<RealtimeSubscriptionDto> subscriptions,
        CancellationToken cancellationToken = default)
    {
        var processed = 0;
        await foreach (var envelope in _realtimeClient.ConnectAsync(subscriptions, cancellationToken))
        {
            if (await _eventJournal.HasProcessedAsync(envelope.EventId, cancellationToken))
            {
                continue;
            }

            await _eventJournal.AppendAsync(envelope, cancellationToken);
            processed++;
        }

        return processed;
    }

    public async Task<int> RunWithReconnectAsync(
        IReadOnlyList<RealtimeSubscriptionDto> subscriptions,
        int maxReconnectAttempts,
        TimeSpan reconnectDelay,
        CancellationToken cancellationToken = default)
    {
        var processed = 0;
        var attempts = 0;

        while (!cancellationToken.IsCancellationRequested && attempts <= maxReconnectAttempts)
        {
            try
            {
                processed += await RunOnceAsync(subscriptions, cancellationToken);
                attempts = 0;
                await Task.Delay(reconnectDelay, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                attempts++;
                if (attempts > maxReconnectAttempts)
                {
                    throw;
                }

                await Task.Delay(reconnectDelay, cancellationToken);
            }
        }

        return processed;
    }

    public async Task<int> ReconcileAsync(
        IReadOnlyList<RealtimeEventEnvelopeDto> authoritativeBacklog,
        CancellationToken cancellationToken = default)
    {
        var replayed = 0;
        foreach (var envelope in authoritativeBacklog.OrderBy(x => x.OccurredAtUtc))
        {
            if (await _eventJournal.HasProcessedAsync(envelope.EventId, cancellationToken))
            {
                continue;
            }

            await _eventJournal.AppendAsync(envelope, cancellationToken);
            replayed++;
        }

        return replayed;
    }
}
