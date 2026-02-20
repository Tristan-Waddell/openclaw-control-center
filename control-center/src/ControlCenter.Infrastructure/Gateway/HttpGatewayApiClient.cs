using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ControlCenter.Application.Abstractions;
using ControlCenter.Contracts;

namespace ControlCenter.Infrastructure.Gateway;

public sealed class HttpGatewayApiClient : IGatewayApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IGatewayConnectionContext _connectionContext;

    public HttpGatewayApiClient(HttpClient httpClient, IGatewayConnectionContext connectionContext)
    {
        _httpClient = httpClient;
        _connectionContext = connectionContext;
    }

    public async Task<GatewayStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await SendAndReadAsync<GatewayStatusDto>(HttpMethod.Get, "api/v1/status", cancellationToken)
            ?? throw new InvalidOperationException("Gateway returned empty status payload.");
    }

    public async Task<IReadOnlyList<AgentSummaryDto>> GetAgentsAsync(CancellationToken cancellationToken = default)
        => await TryGetAsync<List<AgentSummaryDto>>("api/v1/agents", cancellationToken) ?? [];

    public async Task<IReadOnlyList<ProjectSummaryDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
        => await TryGetAsync<List<ProjectSummaryDto>>("api/v1/projects", cancellationToken) ?? [];

    public async Task<IReadOnlyList<TaskRunDto>> GetActiveRunsAsync(CancellationToken cancellationToken = default)
        => await TryGetAsync<List<TaskRunDto>>("api/v1/tasks/active", cancellationToken) ?? [];

    public async Task<UsageSummaryDto> GetUsageSummaryAsync(CancellationToken cancellationToken = default)
        => await TryGetAsync<UsageSummaryDto>("api/v1/usage/summary", cancellationToken) ?? new UsageSummaryDto(0, 0, 0m);

    public async Task<IReadOnlyList<CronJobDto>> GetCronJobsAsync(CancellationToken cancellationToken = default)
        => await TryGetAsync<List<CronJobDto>>("api/v1/cron/jobs", cancellationToken) ?? [];

    public async Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken cancellationToken = default)
        => await TryGetAsync<List<SkillDto>>("api/v1/skills", cancellationToken) ?? [];

    public async Task<IReadOnlyList<ConfigEntryDto>> GetConfigEntriesAsync(CancellationToken cancellationToken = default)
        => await TryGetAsync<List<ConfigEntryDto>>("api/v1/config", cancellationToken) ?? [];

    private async Task<T?> TryGetAsync<T>(string relativeUrl, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Get, relativeUrl);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private async Task<T?> SendAndReadAsync<T>(HttpMethod method, string relativeUrl, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(method, relativeUrl);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

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
