using ControlCenter.Application.Abstractions;
using ControlCenter.Domain;
using Microsoft.Data.Sqlite;

namespace ControlCenter.Infrastructure.Persistence;

public sealed class SqliteHealthRepository : IHealthRepository
{
    private readonly string _connectionString;

    public SqliteHealthRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<SystemHealth> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "select 'ok'";
        var status = (string?)await command.ExecuteScalarAsync(cancellationToken) ?? "degraded";

        return new SystemHealth(status);
    }
}
