using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ControlCenter.Application.Abstractions;
using ControlCenter.Application.Services;
using ControlCenter.Contracts;
using Microsoft.Extensions.Logging;

namespace ControlCenter.UI;

public partial class MainWindow : Window
{
    private readonly HealthService _healthService;
    private readonly IGatewayApiClient _gatewayApiClient;
    private readonly IGatewayCache _gatewayCache;
    private readonly ILogger _logger;

    public ICommand NavigateDashboardCommand { get; }
    public ICommand NavigateAgentsCommand { get; }
    public ICommand RefreshCommand { get; }

    public MainWindow(
        HealthService healthService,
        IGatewayApiClient gatewayApiClient,
        IGatewayCache gatewayCache,
        ILogger logger)
    {
        _healthService = healthService;
        _gatewayApiClient = gatewayApiClient;
        _gatewayCache = gatewayCache;
        _logger = logger;

        NavigateDashboardCommand = new RelayCommand(() => StatusText.Text = "Navigated to Dashboard");
        NavigateAgentsCommand = new RelayCommand(() => StatusText.Text = "Navigated to Agents");
        RefreshCommand = new RelayCommand(async () => await LoadAsync());

        DataContext = this;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            ShowState(loading: true);

            var snapshot = await _healthService.GetSnapshotAsync();
            var gatewayStatus = await _gatewayApiClient.GetStatusAsync();
            var projects = await _gatewayApiClient.GetProjectsAsync();

            await _gatewayCache.SaveStatusAsync(gatewayStatus);
            await _gatewayCache.SaveProjectsAsync(projects);

            GatewayStatusText.Text = snapshot.Status;
            RealtimeStatusText.Text = "Connected (scaffold)";
            CacheStatusText.Text = "SQLite ready";
            TopBarClockText.Text = gatewayStatus.ServerTimeUtc.ToString("u");

            ProjectsGrid.ItemsSource = new ObservableCollection<ProjectSummaryDto>(projects);

            if (projects.Count == 0)
            {
                ShowState(empty: true);
                StatusText.Text = "Loaded: no projects returned.";
            }
            else
            {
                ShowState(content: true);
                StatusText.Text = $"Loaded {projects.Count} project(s).";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Control Center shell state.");
            ErrorText.Text = ex.Message;
            ShowState(error: true);
            StatusText.Text = "Platform status: degraded";
        }
    }

    private void ShowState(bool loading = false, bool empty = false, bool error = false, bool content = false)
    {
        LoadingState.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        ErrorState.Visibility = error ? Visibility.Visible : Visibility.Collapsed;
        ContentState.Visibility = content ? Visibility.Visible : Visibility.Collapsed;
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Func<Task>? _executeAsync;
        private readonly Action? _execute;

        public RelayCommand(Action execute)
        {
            _execute = execute;
        }

        public RelayCommand(Func<Task> executeAsync)
        {
            _executeAsync = executeAsync;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public async void Execute(object? parameter)
        {
            if (_execute is not null)
            {
                _execute();
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (_executeAsync is not null)
            {
                await _executeAsync();
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
