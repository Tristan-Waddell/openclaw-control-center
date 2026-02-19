namespace ControlCenter.Contracts;

public sealed record HealthTileDto(string Title, string Value, string Status, string Detail);

public sealed record NeedsAttentionItemDto(string Category, string Title, string Detail, DateTimeOffset ObservedAtUtc);

public sealed record DashboardSnapshotDto(
    IReadOnlyList<HealthTileDto> Tiles,
    IReadOnlyList<NeedsAttentionItemDto> NeedsAttention,
    DateTimeOffset CapturedAtUtc);