namespace ControlCenter.App.Configuration;

public sealed class EnvironmentProfile
{
    public const string SectionName = "Environment";

    public string Name { get; set; } = "Production";
    public string SqliteConnectionString { get; set; } = "Data Source=control-center.db";
    public string GatewayBaseUrl { get; set; } = "http://localhost:18789/";
    public string? GatewayToken { get; set; }
    public string RealtimeWebSocketUrl { get; set; } = "ws://localhost:18789/realtime";
    public string RealtimeSseUrl { get; set; } = "http://localhost:18789/realtime/sse";
    public string SecretStoreScope { get; set; } = "ControlCenter";
}
