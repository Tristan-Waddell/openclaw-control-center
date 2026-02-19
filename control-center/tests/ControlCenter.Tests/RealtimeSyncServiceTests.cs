using System.Runtime.CompilerServices;
using ControlCenter.Application.Abstractions;
using ControlCenter.Application.Services;
using ControlCenter.Contracts;

namespace ControlCenter.Tests;

public sealed class RealtimeSyncServiceTests
{
    [Fact]
    public async Task RunOnceAsync_SkipsDuplicates()
    {
        var events = new[]
        {
            new RealtimeEventEnvelopeDto("evt-1", "agent.updated", DateTimeOffset.UtcNow, 1, "{}"),
            new RealtimeEventEnvelopeDto("evt-1", "agent.updated", DateTimeOffset.UtcNow, 1, "{}"),
            new RealtimeEventEnvelopeDto("evt-2", "project.updated", DateTimeOffset.UtcNow, 1, "{}")
        };

        var client = new FakeRealtimeClient(events);
        var journal = new FakeEventJournal();
        var service = new RealtimeSyncService(client, journal);

        var processed = await service.RunOnceAsync([new RealtimeSubscriptionDto("agents")]);

        Assert.Equal(2, processed);
        Assert.Equal(2, journal.Stored.Count);
    }

    private sealed class FakeRealtimeClient : IRealtimeClient
    {
        private readonly IReadOnlyList<RealtimeEventEnvelopeDto> _events;

        public FakeRealtimeClient(IReadOnlyList<RealtimeEventEnvelopeDto> events)
        {
            _events = events;
        }

        public async IAsyncEnumerable<RealtimeEventEnvelopeDto> ConnectAsync(
            IReadOnlyList<RealtimeSubscriptionDto> subscriptions,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var envelope in _events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return envelope;
                await Task.Yield();
            }
        }
    }

    private sealed class FakeEventJournal : IEventJournal
    {
        public List<RealtimeEventEnvelopeDto> Stored { get; } = [];

        public Task AppendAsync(RealtimeEventEnvelopeDto envelope, CancellationToken cancellationToken = default)
        {
            Stored.Add(envelope);
            return Task.CompletedTask;
        }

        public Task<bool> HasProcessedAsync(string eventId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Stored.Any(x => x.EventId == eventId));
        }

        public Task<IReadOnlyList<RealtimeEventEnvelopeDto>> ReadRecentAsync(int limit, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<RealtimeEventEnvelopeDto>>(Stored.Take(limit).ToList());
        }
    }
}
