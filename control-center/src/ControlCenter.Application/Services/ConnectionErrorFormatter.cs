using System.Net.Http;
using System.Net.Sockets;

namespace ControlCenter.Application.Services;

public static class ConnectionErrorFormatter
{
    public static string ToUserMessage(Exception exception, string endpoint)
    {
        if (exception is HttpRequestException httpEx &&
            (httpEx.InnerException is SocketException ||
             httpEx.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
             httpEx.Message.Contains("actively refused", StringComparison.OrdinalIgnoreCase) ||
             httpEx.Message.Contains("No connection could be made", StringComparison.OrdinalIgnoreCase)))
        {
            return $"Could not connect to OpenClaw Gateway at {endpoint}.\n" +
                   "Make sure the OpenClaw gateway is running (default: http://localhost:18789) and try Refresh.";
        }

        if (exception is TaskCanceledException)
        {
            return $"The gateway at {endpoint} did not respond in time. Verify it is running and reachable, then try again.";
        }

        return "Control Center could not load dashboard data right now. Check your gateway connection and try again.";
    }
}
