using System.Collections.Generic;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ControlCenter.Application.Abstractions;
using ControlCenter.Contracts;

namespace ControlCenter.Infrastructure.Realtime;

public sealed class GatewayRealtimeClient : IRealtimeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly Uri _webSocketUri;
    private readonly Uri _sseUri;

    public GatewayRealtimeClient(HttpClient httpClient, Uri webSocketUri, Uri sseUri)
    {
        _httpClient = httpClient;
        _webSocketUri = webSocketUri;
        _sseUri = sseUri;
    }

    public async IAsyncEnumerable<RealtimeEventEnvelopeDto> ConnectAsync(
        IReadOnlyList<RealtimeSubscriptionDto> subscriptions,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RealtimeEventEnvelopeDto> events;

        try
        {
            events = await ReadAllAsync(ConnectWebSocketAsync(subscriptions, cancellationToken), cancellationToken);
        }
        catch
        {
            events = await ReadAllAsync(ConnectSseAsync(subscriptions, cancellationToken), cancellationToken);
        }

        foreach (var envelope in events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return envelope;
        }
    }

    private static async Task<IReadOnlyList<RealtimeEventEnvelopeDto>> ReadAllAsync(
        IAsyncEnumerable<RealtimeEventEnvelopeDto> source,
        CancellationToken cancellationToken)
    {
        var output = new List<RealtimeEventEnvelopeDto>();
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            output.Add(item);
        }

        return output;
    }

    private async IAsyncEnumerable<RealtimeEventEnvelopeDto> ConnectWebSocketAsync(
        IReadOnlyList<RealtimeSubscriptionDto> subscriptions,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(_webSocketUri, cancellationToken);

        var payload = JsonSerializer.Serialize(subscriptions, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(payload);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);

        var buffer = new byte[16 * 1024];
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var envelope = JsonSerializer.Deserialize<RealtimeEventEnvelopeDto>(json, JsonOptions);
            if (envelope is not null)
            {
                yield return envelope;
            }
        }
    }

    private async IAsyncEnumerable<RealtimeEventEnvelopeDto> ConnectSseAsync(
        IReadOnlyList<RealtimeSubscriptionDto> subscriptions,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(_sseUri, subscriptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var json = line[5..].Trim();
            var envelope = JsonSerializer.Deserialize<RealtimeEventEnvelopeDto>(json, JsonOptions);
            if (envelope is not null)
            {
                yield return envelope;
            }
        }
    }
}
