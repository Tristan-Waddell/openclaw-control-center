namespace ControlCenter.Contracts;

public sealed record HealthSnapshotDto(string Status, DateTimeOffset CapturedAtUtc);
