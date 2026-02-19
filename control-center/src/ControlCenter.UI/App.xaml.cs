using System.Windows;
using ControlCenter.App;
using ControlCenter.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ControlCenter.UI;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var environment = Environment.GetEnvironmentVariable("CONTROLCENTER_ENV") ?? "Production";
        _serviceProvider = CompositionRoot.BuildServiceProvider(environment);

        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Starting Control Center UI in {Environment}.", environment);

        var healthService = _serviceProvider.GetRequiredService<HealthService>();
        var window = new MainWindow(healthService, logger);
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
