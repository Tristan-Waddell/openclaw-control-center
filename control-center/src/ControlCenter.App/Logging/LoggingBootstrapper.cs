using Microsoft.Extensions.Configuration;
using Serilog;

namespace ControlCenter.App.Logging;

public static class LoggingBootstrapper
{
    public static ILogger CreateLogger(IConfiguration configuration)
    {
        var logPath = configuration["Logging:FilePath"] ?? "logs/control-center-.log";

        return new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}
