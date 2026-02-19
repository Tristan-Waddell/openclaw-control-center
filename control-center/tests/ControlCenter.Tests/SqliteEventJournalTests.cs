using ControlCenter.Contracts;
using ControlCenter.Infrastructure.Persistence;

namespace ControlCenter.Tests;

public sealed class SqliteEventJournalTests
{
    [Fact]
    public async Task AppendAsync_DeduplicatesByEventId()
    {
        var path = Path.Combine(Path.GetTempPath(), $"control-center-{Guid.NewGuid():N}.db");
        var cs = $"Data Source={path};Pooling=False";
        var journal = new SqliteEventJournal(cs);

        var evt = new RealtimeEventEnvelopeDto("evt-1", "agent.updated", DateTimeOffset.UtcNow, 1, "{}", "corr-1");
        await journal.AppendAsync(evt);
        await journal.AppendAsync(evt);

        var recent = await journal.ReadRecentAsync(10);
        Assert.Single(recent);
        Assert.True(await journal.HasProcessedAsync("evt-1"));

        File.Delete(path);
    }
}
