using ControlCenter.Contracts;

namespace ControlCenter.Application.Abstractions;

public interface IEventJournal
{
    Task AppendAsync(RealtimeEventEnvelopeDto envelope, CancellationToken cancellationToken = default);
    Task<bool> HasProcessedAsync(string eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RealtimeEventEnvelopeDto>> ReadRecentAsync(int limit, CancellationToken cancellationToken = default);
}
