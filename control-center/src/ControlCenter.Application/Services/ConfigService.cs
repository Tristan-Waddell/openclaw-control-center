using System.Text.Json;
using ControlCenter.Application.Abstractions;
using ControlCenter.Contracts;

namespace ControlCenter.Application.Services;

public sealed class ConfigService
{
    private readonly IGatewayApiClient _gatewayApiClient;
    private IReadOnlyList<ConfigEntryDto>? _snapshot;

    public ConfigService(IGatewayApiClient gatewayApiClient)
    {
        _gatewayApiClient = gatewayApiClient;
    }

    public Task<IReadOnlyList<ConfigEntryDto>> GetEntriesAsync(CancellationToken cancellationToken = default)
        => _gatewayApiClient.GetConfigEntriesAsync(cancellationToken);

    public bool ValidateEditorPayload(string json) => !string.IsNullOrWhiteSpace(json) && IsJson(json);

    public IReadOnlyList<string> BuildDiff(IReadOnlyList<ConfigEntryDto> current, IReadOnlyList<ConfigEntryDto> proposed)
    {
        var currentMap = current.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        var proposedMap = proposed.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);

        var keys = currentMap.Keys.Union(proposedMap.Keys, StringComparer.Ordinal).OrderBy(key => key);
        return keys
            .Where(key => !string.Equals(currentMap.GetValueOrDefault(key), proposedMap.GetValueOrDefault(key), StringComparison.Ordinal))
            .Select(key => $"{key}: '{currentMap.GetValueOrDefault(key)}' -> '{proposedMap.GetValueOrDefault(key)}'")
            .ToArray();
    }

    public void Snapshot(IReadOnlyList<ConfigEntryDto> current)
    {
        _snapshot = current.Select(item => item with { }).ToArray();
    }

    public IReadOnlyList<ConfigEntryDto> RollbackOrCurrent(IReadOnlyList<ConfigEntryDto> current)
        => _snapshot ?? current;

    private static bool IsJson(string payload)
    {
        try
        {
            JsonDocument.Parse(payload);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}