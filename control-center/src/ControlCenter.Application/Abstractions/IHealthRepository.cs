using ControlCenter.Domain;

namespace ControlCenter.Application.Abstractions;

public interface IHealthRepository
{
    Task<SystemHealth> GetCurrentAsync(CancellationToken cancellationToken = default);
}
