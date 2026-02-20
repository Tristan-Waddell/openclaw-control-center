using System.IO;
using System.Text.Json;
using ControlCenter.Application.Abstractions;

namespace ControlCenter.UI;

public sealed class ConnectionPreferencesStore
{
    private readonly string _path;

    public ConnectionPreferencesStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenClaw",
            "ControlCenter");
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "connection.json");
    }

    public GatewayConnectionOptions? TryLoad()
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<GatewayConnectionOptions>(json);
    }

    public void Save(GatewayConnectionOptions options)
    {
        var json = JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }
}
