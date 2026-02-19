using System.Net.Http.Json;
using ControlCenter.Application.Abstractions;
using ControlCenter.Contracts;

namespace ControlCenter.Infrastructure.Gateway;

public sealed class HttpGatewayApiClient : IGatewayApiClient
{
    private readonly HttpClient _httpClient;

    public HttpGatewayApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GatewayStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<GatewayStatusDto>("api/v1/status", cancellationToken)
            ?? throw new InvalidOperationException("Gateway returned empty status payload.");
    }

    public async Task<IReadOnlyList<AgentSummaryDto>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<AgentSummaryDto>>("api/v1/agents", cancellationToken)
            ?? [];
    }

    public async Task<IReadOnlyList<ProjectSummaryDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<ProjectSummaryDto>>("api/v1/projects", cancellationToken)
            ?? [];
    }
}
