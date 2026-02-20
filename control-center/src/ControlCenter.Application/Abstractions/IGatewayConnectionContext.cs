namespace ControlCenter.Application.Abstractions;

public interface IGatewayConnectionContext
{
    GatewayConnectionOptions Current { get; }

    void Update(GatewayConnectionOptions options);
}
