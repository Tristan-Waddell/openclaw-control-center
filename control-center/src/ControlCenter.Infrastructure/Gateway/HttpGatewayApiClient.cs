using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using ControlCenter.Application.Abstractions;
using ControlCenter.Contracts;

namespace ControlCenter.Infrastructure.Gateway;

public sealed class HttpGatewayApiClient : IGatewayApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IGatewayConnectionContext _connectionContext;

    public HttpGatewayApiClient(HttpClient httpClient, IGatewayConnectionContext connectionContext)
    {
        _httpClient = httpClient;
        _connectionContext = connectionContext;
    }

    public async Task<GatewayStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var result = await InvokeToolResultAsync("session_status", args: null, cancellationToken);

        var model = GetString(result, "model") ?? "unknown";
        var status = GetString(result, "status") ?? "connected";
        var now = DateTimeOffset.UtcNow;

        return new GatewayStatusDto(model, status, now);
    }

    public async Task<IReadOnlyList<AgentSummaryDto>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        var result = await TryInvokeToolResultAsync("agents_list", args: null, cancellationToken);
        if (result is null)
        {
            return [];
        }

        var agentsNode = result["agents"];
        if (agentsNode is JsonArray agentsArray)
        {
            var now = DateTimeOffset.UtcNow;
            return agentsArray
                .Select(MapAgent)
                .Where(agent => agent is not null)
                .Select(agent => agent!)
                .ToArray();
        }

        return [];
    }

    public async Task<IReadOnlyList<ProjectSummaryDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        var result = await TryInvokeToolResultAsync("gateway", new JsonObject
        {
            ["action"] = "config.get",
            ["args"] = new JsonObject
            {
                ["path"] = "agents.list"
            }
        }, cancellationToken);

        if (result is null)
        {
            return [];
        }

        var agents = result["value"] as JsonArray;
        if (agents is null)
        {
            return [];
        }

        return agents
            .Select(MapProject)
            .Where(project => project is not null)
            .Select(project => project!)
            .ToArray();
    }

    public async Task<IReadOnlyList<TaskRunDto>> GetActiveRunsAsync(CancellationToken cancellationToken = default)
    {
        var result = await TryInvokeToolResultAsync("sessions_list", new JsonObject
        {
            ["kinds"] = new JsonArray("agent", "subagent"),
            ["activeMinutes"] = 15,
            ["limit"] = 50,
            ["messageLimit"] = 0
        }, cancellationToken);

        if (result is null)
        {
            return [];
        }

        var sessions = result["sessions"] as JsonArray;
        if (sessions is null)
        {
            return [];
        }

        var now = DateTimeOffset.UtcNow;
        return sessions
            .Select(session => MapTaskRun(session, now))
            .Where(run => run is not null)
            .Select(run => run!)
            .ToArray();
    }

    public async Task<UsageSummaryDto> GetUsageSummaryAsync(CancellationToken cancellationToken = default)
    {
        var result = await TryInvokeToolResultAsync("session_status", args: null, cancellationToken);
        if (result is null)
        {
            return new UsageSummaryDto(0, 0, 0m);
        }

        var usageNode = result["usage"];
        if (usageNode is null)
        {
            return new UsageSummaryDto(0, 0, 0m);
        }

        var prompt = GetInt(usageNode, "promptTokens") ?? GetInt(usageNode, "inputTokens") ?? 0;
        var completion = GetInt(usageNode, "completionTokens") ?? GetInt(usageNode, "outputTokens") ?? 0;
        var estimatedCost = GetDecimal(usageNode, "estimatedCostUsd") ?? 0m;
        return new UsageSummaryDto(prompt, completion, estimatedCost);
    }

    public async Task<IReadOnlyList<CronJobDto>> GetCronJobsAsync(CancellationToken cancellationToken = default)
    {
        var result = await TryInvokeToolResultAsync("cron", new JsonObject { ["action"] = "list" }, cancellationToken);
        if (result is null)
        {
            return [];
        }

        var jobs = result["jobs"] as JsonArray ?? result as JsonArray;
        if (jobs is null)
        {
            return [];
        }

        return jobs
            .Select(MapCronJob)
            .Where(job => job is not null)
            .Select(job => job!)
            .ToArray();
    }

    public async Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        var result = await TryInvokeToolResultAsync("skills", new JsonObject { ["action"] = "list" }, cancellationToken);
        if (result is null)
        {
            return [];
        }

        var skills = result["skills"] as JsonArray ?? result as JsonArray;
        if (skills is null)
        {
            return [];
        }

        return skills
            .Select(MapSkill)
            .Where(skill => skill is not null)
            .Select(skill => skill!)
            .ToArray();
    }

    public async Task<IReadOnlyList<ConfigEntryDto>> GetConfigEntriesAsync(CancellationToken cancellationToken = default)
    {
        var result = await TryInvokeToolResultAsync("gateway", new JsonObject { ["action"] = "config.get" }, cancellationToken);
        if (result is null)
        {
            return [];
        }

        var value = result["value"];
        if (value is null)
        {
            return [];
        }

        var entries = new List<ConfigEntryDto>();
        FlattenConfig(value, string.Empty, entries);
        return entries;
    }

    private async Task<JsonNode?> TryInvokeToolResultAsync(string tool, JsonObject? args, CancellationToken cancellationToken)
    {
        try
        {
            return await InvokeToolResultAsync(tool, args, cancellationToken);
        }
        catch (GatewayApiCompatibilityException)
        {
            return null;
        }
    }

    private async Task<JsonNode?> InvokeToolResultAsync(string tool, JsonObject? args, CancellationToken cancellationToken)
    {
        var payload = new JsonObject
        {
            ["tool"] = tool
        };

        if (args is not null)
        {
            foreach (var (key, value) in args)
            {
                payload[key] = value?.DeepClone();
            }
        }

        using var request = BuildRequest(HttpMethod.Post, "tools/invoke");
        request.Content = JsonContent.Create(payload, options: JsonOptions);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new GatewayApiCompatibilityException("Gateway does not expose the supported tools API endpoint.");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.EnsureSuccessStatusCode();
        }

        var root = await ReadJsonContentAsync<JsonNode>(response, cancellationToken);
        if (root is null)
        {
            throw new GatewayApiCompatibilityException("Gateway returned an empty tools API response.");
        }

        var ok = root["ok"]?.GetValue<bool?>();
        if (ok != true)
        {
            var errorType = GetString(root, "error", "type");
            if (response.StatusCode == HttpStatusCode.NotFound || string.Equals(errorType, "not_found", StringComparison.OrdinalIgnoreCase))
            {
                throw new GatewayApiCompatibilityException($"Gateway tool '{tool}' is unavailable.");
            }

            var message = GetString(root, "error", "message") ?? "Gateway reported a tools API error.";
            throw new GatewayApiCompatibilityException(message);
        }

        return root["result"];
    }

    private static AgentSummaryDto? MapAgent(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        var id = GetString(node, "id") ?? GetString(node, "name") ?? Guid.NewGuid().ToString("n");
        var name = GetString(node, "name") ?? id;
        var status = GetBool(node, "active") == true ? "active" : "online";

        return new AgentSummaryDto(id, name, status, DateTimeOffset.UtcNow);
    }

    private static ProjectSummaryDto? MapProject(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        var id = GetString(node, "id") ?? Guid.NewGuid().ToString("n");
        var workspace = GetString(node, "workspace");
        var name = !string.IsNullOrWhiteSpace(workspace)
            ? Path.GetFileName(workspace.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            : id;

        var branch = GetString(node, "branch") ?? "unknown";
        var commit = GetString(node, "commit") ?? "unknown";

        return new ProjectSummaryDto(id, name, branch, commit, "ok");
    }

    private static TaskRunDto? MapTaskRun(JsonNode? node, DateTimeOffset fallbackStart)
    {
        if (node is null)
        {
            return null;
        }

        var id = GetString(node, "id") ?? GetString(node, "sessionKey") ?? Guid.NewGuid().ToString("n");
        var name = GetString(node, "title") ?? GetString(node, "sessionKey") ?? id;
        var state = GetString(node, "state") ?? "active";
        var started = GetDate(node, "updatedAt") ?? GetDate(node, "createdAt") ?? fallbackStart;

        return new TaskRunDto(id, name, state, started);
    }

    private static CronJobDto? MapCronJob(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        var id = GetString(node, "id") ?? Guid.NewGuid().ToString("n");
        var name = GetString(node, "name") ?? id;
        var enabled = GetBool(node, "enabled") ?? true;
        var schedule = GetString(node, "schedule") ?? GetString(node, "cron") ?? "unknown";
        var lastRun = GetDate(node, "lastRunAt") ?? GetDate(node, "lastRunUtc");

        return new CronJobDto(id, name, enabled, schedule, lastRun);
    }

    private static SkillDto? MapSkill(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        var id = GetString(node, "id") ?? GetString(node, "name") ?? Guid.NewGuid().ToString("n");
        var name = GetString(node, "name") ?? id;
        var enabled = GetBool(node, "enabled") ?? GetBool(node, "eligible") ?? true;
        var source = GetString(node, "source") ?? "gateway";
        var health = enabled ? "ok" : "disabled";

        return new SkillDto(id, name, enabled, source, health);
    }

    private static void FlattenConfig(JsonNode node, string prefix, ICollection<ConfigEntryDto> entries)
    {
        if (node is JsonObject obj)
        {
            foreach (var (key, value) in obj)
            {
                var path = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";
                if (value is null)
                {
                    entries.Add(new ConfigEntryDto(path, "null", IsSensitive(path), "gateway"));
                    continue;
                }

                FlattenConfig(value, path, entries);
            }

            return;
        }

        if (node is JsonArray array)
        {
            for (var i = 0; i < array.Count; i++)
            {
                var path = $"{prefix}[{i}]";
                var value = array[i];
                if (value is null)
                {
                    entries.Add(new ConfigEntryDto(path, "null", IsSensitive(path), "gateway"));
                    continue;
                }

                FlattenConfig(value, path, entries);
            }

            return;
        }

        entries.Add(new ConfigEntryDto(prefix, node.ToJsonString(), IsSensitive(prefix), "gateway"));
    }

    private static bool IsSensitive(string key)
        => key.Contains("token", StringComparison.OrdinalIgnoreCase)
           || key.Contains("password", StringComparison.OrdinalIgnoreCase)
           || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
           || key.Contains("apikey", StringComparison.OrdinalIgnoreCase)
           || key.Contains("api_key", StringComparison.OrdinalIgnoreCase);

    private static string? GetString(JsonNode? node, params string[] path)
    {
        var current = Traverse(node, path);
        return current switch
        {
            null => null,
            JsonValue value when value.TryGetValue<string>(out var text) => text,
            _ => current.ToJsonString().Trim('"')
        };
    }

    private static int? GetInt(JsonNode? node, params string[] path)
    {
        var current = Traverse(node, path);
        if (current is not JsonValue value)
        {
            return null;
        }

        if (value.TryGetValue<int>(out var intValue))
        {
            return intValue;
        }

        return value.TryGetValue<long>(out var longValue) ? (int?)longValue : null;
    }

    private static decimal? GetDecimal(JsonNode? node, params string[] path)
    {
        var current = Traverse(node, path);
        if (current is not JsonValue value)
        {
            return null;
        }

        if (value.TryGetValue<decimal>(out var decimalValue))
        {
            return decimalValue;
        }

        if (value.TryGetValue<double>(out var doubleValue))
        {
            return (decimal)doubleValue;
        }

        return null;
    }

    private static bool? GetBool(JsonNode? node, params string[] path)
    {
        var current = Traverse(node, path);
        return current is JsonValue value && value.TryGetValue<bool>(out var boolValue)
            ? boolValue
            : null;
    }

    private static DateTimeOffset? GetDate(JsonNode? node, params string[] path)
    {
        var text = GetString(node, path);
        return DateTimeOffset.TryParse(text, out var value) ? value : null;
    }

    private static JsonNode? Traverse(JsonNode? node, params string[] path)
    {
        var current = node;
        foreach (var segment in path)
        {
            current = current?[segment];
            if (current is null)
            {
                return null;
            }
        }

        return current;
    }

    private async Task<T?> ReadJsonContentAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (LooksLikeHtmlMediaType(mediaType))
        {
            throw new GatewayApiCompatibilityException("Gateway returned HTML instead of tools API JSON.");
        }

        try
        {
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is NotSupportedException || ex is JsonException)
        {
            throw new GatewayApiCompatibilityException("Gateway returned an incompatible tools API response.", ex);
        }
    }

    private static bool LooksLikeHtmlMediaType(string? mediaType)
        => !string.IsNullOrWhiteSpace(mediaType) && mediaType.Contains("html", StringComparison.OrdinalIgnoreCase);

    private HttpRequestMessage BuildRequest(HttpMethod method, string relativeUrl)
    {
        var current = _connectionContext.Current;
        var baseUri = new Uri(NormalizeBaseUrl(current.BaseUrl), UriKind.Absolute);
        var request = new HttpRequestMessage(method, new Uri(baseUri, relativeUrl));

        if (!string.IsNullOrWhiteSpace(current.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", current.Token);
        }

        return request;
    }

    private static string NormalizeBaseUrl(string baseUrl)
        => baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/";
}
