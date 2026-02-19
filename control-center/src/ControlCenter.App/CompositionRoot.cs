using ControlCenter.App.DependencyInjection;
using ControlCenter.App.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ControlCenter.App;

public static class CompositionRoot
{
    public static ServiceProvider BuildServiceProvider(string environment)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        var logger = LoggingBootstrapper.CreateLogger(configuration);

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog(logger, dispose: true));
        services.AddControlCenter(configuration);

        return services.BuildServiceProvider();
    }
}
