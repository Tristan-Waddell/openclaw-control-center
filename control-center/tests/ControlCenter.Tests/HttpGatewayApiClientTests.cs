using System.Net;
using System.Net.Http;
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
                Content = new StringContent("<html><body>OpenClaw Dashboard</body></html>")
            });

        using var httpClient = new HttpClient(handler);
        var client = new HttpGatewayApiClient(httpClient, new TestConnectionContext());

        await Assert.ThrowsAsync<GatewayApiCompatibilityException>(() => client.GetStatusAsync());
    }

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
