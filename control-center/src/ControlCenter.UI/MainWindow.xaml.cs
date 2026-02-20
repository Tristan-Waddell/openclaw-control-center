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
    private readonly DashboardService _dashboardService;
    private readonly IGatewayApiClient _gatewayApiClient;
    private readonly IGatewayConnectionContext _connectionContext;
    private readonly ConnectionPreferencesStore _preferencesStore;
    private readonly ILogger _logger;

    public ICommand NavigateDashboardCommand { get; }
    public ICommand NavigateAgentsCommand { get; }
    public ICommand RefreshCommand { get; }

    public MainWindow(
        DashboardService dashboardService,
        IGatewayApiClient gatewayApiClient,
        IGatewayConnectionContext connectionContext,
        ILogger logger)
    {
        _dashboardService = dashboardService;
        _gatewayApiClient = gatewayApiClient;
        _connectionContext = connectionContext;
        _preferencesStore = new ConnectionPreferencesStore();
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
        var saved = _preferencesStore.TryLoad();
        if (saved is not null)
        {
            _connectionContext.Update(saved);
        }

        ApplyInputsFromContext();
        await AutoDetectLocalGatewayIfNeededAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            SetConnectionState("Connecting", connected: false);
            ShowState(loading: true);

            var snapshot = await _dashboardService.GetSnapshotAsync();

            TopBarClockText.Text = snapshot.CapturedAtUtc.ToString("u");
            HealthTilesItems.ItemsSource = new ObservableCollection<HealthTileDto>(snapshot.Tiles);
            NeedsAttentionGrid.ItemsSource = new ObservableCollection<NeedsAttentionItemDto>(snapshot.NeedsAttention);

            if (snapshot.Tiles.Count == 0)
            {
                ShowState(empty: true);
                StatusText.Text = "Loaded: no dashboard data.";
            }
            else
            {
                ShowState(content: true);
                StatusText.Text = snapshot.NeedsAttention.Count == 0
                    ? "Dashboard healthy."
                    : $"{snapshot.NeedsAttention.Count} item(s) need attention.";
            }

            SetConnectionState("Connected", connected: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load dashboard state.");
            var failureKind = ConnectionErrorFormatter.Classify(ex);
            ErrorText.Text = ConnectionErrorFormatter.ToUserMessage(ex, _connectionContext.Current.BaseUrl);
            ShowState(error: true);

            if (failureKind == ConnectionFailureKind.IncompatibleApi)
            {
                StatusText.Text = "Dashboard status: gateway reachable, API incompatible";
                SetConnectionState("Incompatible API", connected: false);
                return;
            }

            StatusText.Text = "Dashboard status: disconnected";
            SetConnectionState("Disconnected", connected: false);
        }
    }

    private void ShowState(bool loading = false, bool empty = false, bool error = false, bool content = false)
    {
        LoadingState.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
        EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        ErrorState.Visibility = error ? Visibility.Visible : Visibility.Collapsed;
        ContentState.Visibility = content ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetConnectionState(string state, bool connected)
    {
        var current = _connectionContext.Current;
        TopBarConnectionStateText.Text = state;
        TopBarEndpointText.Text = current.BaseUrl;
        ConnectPanel.Visibility = connected ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ApplyInputsFromContext()
    {
        var current = _connectionContext.Current;
        GatewayUrlInput.Text = current.BaseUrl;
        GatewayTokenInput.Text = current.Token ?? string.Empty;
        TopBarEndpointText.Text = current.BaseUrl;
    }

    private GatewayConnectionOptions ReadOptionsFromInputs()
    {
        var url = GatewayUrlInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            url = "http://localhost:18789/";
        }

        if (!url.EndsWith('/'))
        {
            url = $"{url}/";
        }

        return new GatewayConnectionOptions(url, string.IsNullOrWhiteSpace(GatewayTokenInput.Text) ? null : GatewayTokenInput.Text.Trim());
    }

    private async Task<bool> TestConnectionAsync(GatewayConnectionOptions options)
    {
        var prior = _connectionContext.Current;
        _connectionContext.Update(options);

        try
        {
            await _gatewayApiClient.GetStatusAsync();
            return true;
        }
        catch
        {
            _connectionContext.Update(prior);
            return false;
        }
    }

    private async void OnTestConnectionClick(object sender, RoutedEventArgs e)
    {
        var options = ReadOptionsFromInputs();
        var ok = await TestConnectionAsync(options);
        StatusText.Text = ok ? "Connection test successful." : "Connection test failed.";
        if (!ok)
        {
            SetConnectionState("Disconnected", connected: false);
        }
    }

    private async void OnSaveConnectionClick(object sender, RoutedEventArgs e)
    {
        var options = ReadOptionsFromInputs();
        _connectionContext.Update(options);
        _preferencesStore.Save(options);
        await LoadAsync();
    }

    private async void OnAutoDetectConnectionClick(object sender, RoutedEventArgs e)
    {
        var detected = await TryDetectLocalGatewayAsync();
        if (detected is null)
        {
            StatusText.Text = "Local gateway auto-detect failed.";
            return;
        }

        GatewayUrlInput.Text = detected.BaseUrl;
        if (!string.IsNullOrWhiteSpace(detected.Token))
        {
            GatewayTokenInput.Text = detected.Token;
        }

        StatusText.Text = $"Detected local gateway at {detected.BaseUrl}";
    }

    private async void OnChangeConnectionClick(object sender, RoutedEventArgs e)
    {
        ConnectPanel.Visibility = Visibility.Visible;
        StatusText.Text = "Update your gateway connection settings.";
        await Task.CompletedTask;
    }

    private async Task AutoDetectLocalGatewayIfNeededAsync()
    {
        if (!string.Equals(_connectionContext.Current.BaseUrl, "http://localhost:18789/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var detected = await TryDetectLocalGatewayAsync();
        if (detected is not null)
        {
            _connectionContext.Update(detected);
            ApplyInputsFromContext();
        }
    }

    private async Task<GatewayConnectionOptions?> TryDetectLocalGatewayAsync()
    {
        var candidate = new GatewayConnectionOptions("http://localhost:18789/", GatewayTokenInput.Text?.Trim());
        return await TestConnectionAsync(candidate) ? candidate : null;
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Func<Task>? _executeAsync;
        private readonly Action? _execute;

        public RelayCommand(Action execute) => _execute = execute;
        public RelayCommand(Func<Task> executeAsync) => _executeAsync = executeAsync;

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
