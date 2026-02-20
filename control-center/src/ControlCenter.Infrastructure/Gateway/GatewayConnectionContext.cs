using ControlCenter.Application.Abstractions;

namespace ControlCenter.Infrastructure.Gateway;

public sealed class GatewayConnectionContext : IGatewayConnectionContext
{
    private readonly object _gate = new();
    private GatewayConnectionOptions _current;

    public GatewayConnectionContext(GatewayConnectionOptions initial)
    {
        _current = initial;
    }

    public GatewayConnectionOptions Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public void Update(GatewayConnectionOptions options)
    {
        lock (_gate)
        {
            _current = options;
        }
    }
}
