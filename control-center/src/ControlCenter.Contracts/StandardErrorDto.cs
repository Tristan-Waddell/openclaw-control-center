namespace ControlCenter.Contracts;

public sealed record GatewayErrorDto(
    string Code,
    string Message,
    string? Details,
    bool Retryable,
    string CorrelationId);
