namespace ControlCenter.Application.Abstractions;

public sealed class GatewayApiCompatibilityException : Exception
{
    public GatewayApiCompatibilityException(string message)
        : base(message)
    {
    }

    public GatewayApiCompatibilityException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
