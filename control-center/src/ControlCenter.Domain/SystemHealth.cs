namespace ControlCenter.Domain;

public sealed class SystemHealth
{
    public SystemHealth(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status is required.", nameof(status));
        }

        Status = status;
        CapturedAtUtc = DateTimeOffset.UtcNow;
    }

    public string Status { get; }
    public DateTimeOffset CapturedAtUtc { get; }
}
