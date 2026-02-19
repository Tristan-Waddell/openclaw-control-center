namespace ControlCenter.Contracts;

public sealed record TaskRunDto(string Id, string AgentName, string State, DateTimeOffset StartedAtUtc);

public sealed record UsageSummaryDto(int PromptTokens, int CompletionTokens, decimal EstimatedCostUsd);

public sealed record CronJobDto(string Id, string Name, bool Enabled, string Schedule, DateTimeOffset? LastRunUtc);

public sealed record SkillDto(string Id, string Name, bool Enabled, string Source, string Health);

public sealed record ConfigEntryDto(string Key, string Value, bool IsSensitive, string Source);