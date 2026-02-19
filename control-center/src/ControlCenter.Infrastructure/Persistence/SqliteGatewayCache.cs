using ControlCenter.Application.Abstractions;
using ControlCenter.Contracts;
using Microsoft.Data.Sqlite;

namespace ControlCenter.Infrastructure.Persistence;

public sealed class SqliteGatewayCache : IGatewayCache
{
    private readonly string _connectionString;

    public SqliteGatewayCache(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SaveStatusAsync(GatewayStatusDto status, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
insert into gateway_status (id, version, environment, server_time_utc)
values (1, $version, $environment, $serverTime)
on conflict(id) do update set
  version = excluded.version,
  environment = excluded.environment,
  server_time_utc = excluded.server_time_utc;";
        command.Parameters.AddWithValue("$version", status.Version);
        command.Parameters.AddWithValue("$environment", status.Environment);
        command.Parameters.AddWithValue("$serverTime", status.ServerTimeUtc.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SaveAgentsAsync(IReadOnlyList<AgentSummaryDto> agents, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var clear = connection.CreateCommand();
        clear.CommandText = "delete from cache_agents;";
        await clear.ExecuteNonQueryAsync(cancellationToken);

        foreach (var agent in agents)
        {
            var insert = connection.CreateCommand();
            insert.CommandText = @"
insert into cache_agents (id, name, status, last_heartbeat_utc)
values ($id, $name, $status, $heartbeat);";
            insert.Parameters.AddWithValue("$id", agent.Id);
            insert.Parameters.AddWithValue("$name", agent.Name);
            insert.Parameters.AddWithValue("$status", agent.Status);
            insert.Parameters.AddWithValue("$heartbeat", agent.LastHeartbeatUtc.ToString("O"));
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SaveProjectsAsync(IReadOnlyList<ProjectSummaryDto> projects, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var clear = connection.CreateCommand();
        clear.CommandText = "delete from cache_projects;";
        await clear.ExecuteNonQueryAsync(cancellationToken);

        foreach (var project in projects)
        {
            var insert = connection.CreateCommand();
            insert.CommandText = @"
insert into cache_projects (id, name, branch, commit_sha, health)
values ($id, $name, $branch, $sha, $health);";
            insert.Parameters.AddWithValue("$id", project.Id);
            insert.Parameters.AddWithValue("$name", project.Name);
            insert.Parameters.AddWithValue("$branch", project.Branch);
            insert.Parameters.AddWithValue("$sha", project.CommitSha);
            insert.Parameters.AddWithValue("$health", project.Health);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<GatewayStatusDto?> ReadStatusAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "select version, environment, server_time_utc from gateway_status where id = 1;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new GatewayStatusDto(
            reader.GetString(0),
            reader.GetString(1),
            DateTimeOffset.Parse(reader.GetString(2)));
    }

    public async Task<IReadOnlyList<AgentSummaryDto>> ReadAgentsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "select id, name, status, last_heartbeat_utc from cache_agents order by name;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var agents = new List<AgentSummaryDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            agents.Add(new AgentSummaryDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                DateTimeOffset.Parse(reader.GetString(3))));
        }

        return agents;
    }

    public async Task<IReadOnlyList<ProjectSummaryDto>> ReadProjectsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "select id, name, branch, commit_sha, health from cache_projects order by name;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var projects = new List<ProjectSummaryDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            projects.Add(new ProjectSummaryDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4)));
        }

        return projects;
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
create table if not exists gateway_status (
  id integer primary key,
  version text not null,
  environment text not null,
  server_time_utc text not null
);

create table if not exists cache_agents (
  id text primary key,
  name text not null,
  status text not null,
  last_heartbeat_utc text not null
);

create table if not exists cache_projects (
  id text primary key,
  name text not null,
  branch text not null,
  commit_sha text not null,
  health text not null
);

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
