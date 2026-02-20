namespace ControlCenter.Tests;

public sealed class UiSmokeTests
{
    [Fact]
    public void MainWindowXaml_ContainsCoreRoutes()
    {
        var root = LocateRepositoryRoot();
        var xamlPath = Path.Combine(root, "src", "ControlCenter.UI", "MainWindow.xaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("Dashboard", xaml, StringComparison.Ordinal);
        Assert.Contains("Agents", xaml, StringComparison.Ordinal);
        Assert.Contains("Projects", xaml, StringComparison.Ordinal);
        Assert.Contains("Settings", xaml, StringComparison.Ordinal);
        Assert.Contains("Connect OpenClaw", xaml, StringComparison.Ordinal);
        Assert.Contains("Save &amp; Connect", xaml, StringComparison.Ordinal);
        Assert.Contains("Change Connection", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void AppSettings_DefaultGatewayPort_UsesOpenClawPort()
    {
        var root = LocateRepositoryRoot();
        var appSettingsPath = Path.Combine(root, "src", "ControlCenter.UI", "appsettings.json");
        var appSettings = File.ReadAllText(appSettingsPath);

        Assert.Contains("http://localhost:18789/", appSettings, StringComparison.Ordinal);
    }

    private static string LocateRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ControlCenter.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
