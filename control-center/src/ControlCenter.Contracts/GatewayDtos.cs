namespace ControlCenter.Contracts;

public sealed record AgentSummaryDto(string Id, string Name, string Status, DateTimeOffset LastHeartbeatUtc);

public sealed record ProjectSummaryDto(string Id, string Name, string Branch, string CommitSha, string Health);

public sealed record GatewayStatusDto(string Version, string Environment, DateTimeOffset ServerTimeUtc);

public sealed record RealtimeSubscriptionDto(string Channel, string? Cursor = null);

public sealed record RealtimeEventEnvelopeDto(
    string EventId,
    string EventType,
    DateTimeOffset OccurredAtUtc,
    int Version,
    string PayloadJson,
    string? CorrelationId = null);
