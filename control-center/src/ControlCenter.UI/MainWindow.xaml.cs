using System.Windows;
using ControlCenter.Application.Services;
using Microsoft.Extensions.Logging;

namespace ControlCenter.UI;

public partial class MainWindow : Window
{
    private readonly HealthService _healthService;
    private readonly ILogger _logger;

    public MainWindow(HealthService healthService, ILogger logger)
    {
        _healthService = healthService;
        _logger = logger;

        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var snapshot = await _healthService.GetSnapshotAsync();
            StatusText.Text = $"Platform status: {snapshot.Status} @ {snapshot.CapturedAtUtc:O}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load startup health snapshot.");
            StatusText.Text = "Platform status: degraded";
        }
    }
}
