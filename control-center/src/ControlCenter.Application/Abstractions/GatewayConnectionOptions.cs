namespace ControlCenter.Application.Abstractions;

public sealed record GatewayConnectionOptions(string BaseUrl, string? Token = null);
