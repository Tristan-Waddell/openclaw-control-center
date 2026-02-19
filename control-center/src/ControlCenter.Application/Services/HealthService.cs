using ControlCenter.Application.Abstractions;
using ControlCenter.Contracts;

namespace ControlCenter.Application.Services;

public sealed class HealthService
{
    private readonly IHealthRepository _repository;

    public HealthService(IHealthRepository repository)
    {
        _repository = repository;
    }

    public async Task<HealthSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var model = await _repository.GetCurrentAsync(cancellationToken);
        return new HealthSnapshotDto(model.Status, model.CapturedAtUtc);
    }
}
