using ControlCenter.Application.Abstractions;
using ControlCenter.Contracts;
using Microsoft.Data.Sqlite;

namespace ControlCenter.Infrastructure.Persistence;

public sealed class SqliteEventJournal : IEventJournal
{
    private readonly string _connectionString;

    public SqliteEventJournal(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task AppendAsync(RealtimeEventEnvelopeDto envelope, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
insert into event_journal (
  event_id,
  event_type,
  occurred_at_utc,
  version,
  payload_json,
  correlation_id,
  inserted_at_utc)
values ($eventId, $eventType, $occurredAt, $version, $payloadJson, $correlationId, $insertedAt)
on conflict(event_id) do nothing;";

        command.Parameters.AddWithValue("$eventId", envelope.EventId);
        command.Parameters.AddWithValue("$eventType", envelope.EventType);
        command.Parameters.AddWithValue("$occurredAt", envelope.OccurredAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$version", envelope.Version);
        command.Parameters.AddWithValue("$payloadJson", envelope.PayloadJson);
        command.Parameters.AddWithValue("$correlationId", (object?)envelope.CorrelationId ?? DBNull.Value);
        command.Parameters.AddWithValue("$insertedAt", DateTimeOffset.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> HasProcessedAsync(string eventId, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "select 1 from event_journal where event_id = $eventId limit 1;";
        command.Parameters.AddWithValue("$eventId", eventId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    public async Task<IReadOnlyList<RealtimeEventEnvelopeDto>> ReadRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
select event_id, event_type, occurred_at_utc, version, payload_json, correlation_id
from event_journal
order by inserted_at_utc desc
limit $limit;";
        command.Parameters.AddWithValue("$limit", Math.Max(1, limit));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var rows = new List<RealtimeEventEnvelopeDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new RealtimeEventEnvelopeDto(
                reader.GetString(0),
                reader.GetString(1),
                DateTimeOffset.Parse(reader.GetString(2)),
                reader.GetInt32(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5)));
        }

        return rows;
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
create table if not exists event_journal (
  event_id text primary key,
  event_type text not null,
  occurred_at_utc text not null,
  version integer not null,
  payload_json text not null,
  correlation_id text null,
  inserted_at_utc text not null
);";

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
