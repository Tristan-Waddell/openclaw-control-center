using System.Net;
using System.Net.Http;
using System.Text;
using ControlCenter.Application.Abstractions;
using ControlCenter.Infrastructure.Gateway;

namespace ControlCenter.Tests;

public sealed class HttpGatewayApiClientTests
{
    [Fact]
    public async Task GetStatusAsync_HtmlResponse_ThrowsCompatibilityException()
    {
        var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html><body>OpenClaw Dashboard</body></html>", Encoding.UTF8, "text/html")
            });

        using var httpClient = new HttpClient(handler);
        var client = new HttpGatewayApiClient(httpClient, new TestConnectionContext());

        await Assert.ThrowsAsync<GatewayApiCompatibilityException>(() => client.GetStatusAsync());
    }

    [Fact]
    public async Task GetStatusAsync_ToolsInvokeResponse_MapsConnectedStatus()
    {
        var handler = new StubHandler(_ => JsonResponse("""
            { "ok": true, "result": { "model": "gpt-5", "status": "ready" } }
            """));

        using var httpClient = new HttpClient(handler);
        var client = new HttpGatewayApiClient(httpClient, new TestConnectionContext());

        var status = await client.GetStatusAsync();

        Assert.Equal("gpt-5", status.Version);
        Assert.Equal("ready", status.Environment);
    }

    [Fact]
    public async Task GetCronJobsAsync_ToolUnavailable_ReturnsEmpty()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{ \"ok\": false, \"error\": { \"type\": \"not_found\" } }")
        });

        using var httpClient = new HttpClient(handler);
        var client = new HttpGatewayApiClient(httpClient, new TestConnectionContext());

        var jobs = await client.GetCronJobsAsync();

        Assert.Empty(jobs);
    }

    private static HttpResponseMessage JsonResponse(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class TestConnectionContext : IGatewayConnectionContext
    {
        public GatewayConnectionOptions Current { get; private set; } = new("http://localhost:18789/");

        public void Update(GatewayConnectionOptions options) => Current = options;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _factory;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> factory)
        {
            _factory = factory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_factory(request));
    }
}
