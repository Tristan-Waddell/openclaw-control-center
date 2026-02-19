using ControlCenter.Contracts;

namespace ControlCenter.Application.Abstractions;

public interface IRealtimeClient
{
    IAsyncEnumerable<RealtimeEventEnvelopeDto> ConnectAsync(
        IReadOnlyList<RealtimeSubscriptionDto> subscriptions,
        CancellationToken cancellationToken = default);
}
