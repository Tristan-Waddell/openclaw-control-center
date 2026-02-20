using System.Net.Http;
using System.Net.Sockets;
using ControlCenter.Application.Abstractions;
using ControlCenter.Application.Services;

namespace ControlCenter.Tests;

public sealed class ConnectionErrorFormatterTests
{
    [Fact]
    public void ToUserMessage_ConnectionRefused_ReturnsActionableMessage()
    {
        var ex = new HttpRequestException("Connection refused", new SocketException((int)SocketError.ConnectionRefused));

        var message = ConnectionErrorFormatter.ToUserMessage(ex, "http://localhost:18789/");

        Assert.Contains("Could not connect to OpenClaw Gateway", message, StringComparison.Ordinal);
        Assert.Contains("http://localhost:18789/", message, StringComparison.Ordinal);
    }

    [Fact]
    public void ToUserMessage_IncompatibleApi_ReturnsCompatibilityMessage()
    {
        var ex = new GatewayApiCompatibilityException("JSON parse failed");

        var message = ConnectionErrorFormatter.ToUserMessage(ex, "http://localhost:18789/");

        Assert.Contains("not serving a compatible dashboard API", message, StringComparison.Ordinal);
        Assert.DoesNotContain("JSON parse failed", message, StringComparison.Ordinal);
        Assert.Equal(ConnectionFailureKind.IncompatibleApi, ConnectionErrorFormatter.Classify(ex));
    }

    [Fact]
    public void ToUserMessage_UnknownError_DoesNotLeakRawException()
    {
        var ex = new InvalidOperationException("Sensitive internals");

        var message = ConnectionErrorFormatter.ToUserMessage(ex, "http://localhost:18789/");

        Assert.DoesNotContain("Sensitive internals", message, StringComparison.Ordinal);
    }
}
