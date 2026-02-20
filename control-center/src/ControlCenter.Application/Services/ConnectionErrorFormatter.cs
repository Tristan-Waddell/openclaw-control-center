using System.Net.Http;
using System.Net.Sockets;
using ControlCenter.Application.Abstractions;

namespace ControlCenter.Application.Services;

public static class ConnectionErrorFormatter
{
    public static ConnectionFailureKind Classify(Exception exception)
    {
        if (exception is GatewayApiCompatibilityException)
        {
            return ConnectionFailureKind.IncompatibleApi;
        }

        if (exception is HttpRequestException httpEx &&
            (httpEx.InnerException is SocketException ||
             httpEx.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
             httpEx.Message.Contains("actively refused", StringComparison.OrdinalIgnoreCase) ||
             httpEx.Message.Contains("No connection could be made", StringComparison.OrdinalIgnoreCase)))
        {
            return ConnectionFailureKind.Unreachable;
        }

        if (exception is TaskCanceledException)
        {
            return ConnectionFailureKind.Timeout;
        }

        return ConnectionFailureKind.Unknown;
    }

    public static string ToUserMessage(Exception exception, string endpoint)
    {
        return Classify(exception) switch
        {
            ConnectionFailureKind.IncompatibleApi =>
                $"Connected to gateway at {endpoint}, but it is not serving a compatible dashboard API. " +
                "Point Control Center to an OpenClaw gateway API endpoint or update to compatible versions.",
            ConnectionFailureKind.Unreachable =>
                $"Could not connect to OpenClaw Gateway at {endpoint}.\n" +
                "Make sure the OpenClaw gateway is running (default: http://localhost:18789) and try Refresh.",
            ConnectionFailureKind.Timeout =>
                $"The gateway at {endpoint} did not respond in time. Verify it is running and reachable, then try again.",
            _ => "Control Center could not load dashboard data right now. Check your gateway connection and try again."
        };
    }
}

public enum ConnectionFailureKind
{
    Unknown = 0,
    Unreachable = 1,
    Timeout = 2,
    IncompatibleApi = 3
}
