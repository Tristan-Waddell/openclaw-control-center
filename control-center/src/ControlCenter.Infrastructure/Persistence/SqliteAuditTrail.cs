using ControlCenter.Application.Abstractions;
using Microsoft.Data.Sqlite;

namespace ControlCenter.Infrastructure.Persistence;

public sealed class SqliteAuditTrail : IAuditTrail
{
    private readonly string _connectionString;

    public SqliteAuditTrail(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task RecordMutationAsync(string actor, string action, string target, string details, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
insert into mutation_audit(actor, action, target, details, created_at_utc)
values($actor, $action, $target, $details, $createdAt);";
        command.Parameters.AddWithValue("$actor", actor);
        command.Parameters.AddWithValue("$action", action);
        command.Parameters.AddWithValue("$target", target);
        command.Parameters.AddWithValue("$details", details);
        command.Parameters.AddWithValue("$createdAt", DateTimeOffset.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
create table if not exists mutation_audit (
  id integer primary key autoincrement,
  actor text not null,
  action text not null,
  target text not null,
  details text not null,
  created_at_utc text not null
);";

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}