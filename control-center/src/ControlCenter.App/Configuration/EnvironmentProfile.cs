namespace ControlCenter.App.Configuration;

public sealed class EnvironmentProfile
{
    public const string SectionName = "Environment";

    public string Name { get; set; } = "Production";
    public string SqliteConnectionString { get; set; } = "Data Source=control-center.db";
}
