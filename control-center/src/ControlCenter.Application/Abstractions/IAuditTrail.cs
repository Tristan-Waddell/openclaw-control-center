namespace ControlCenter.Application.Abstractions;

public interface IAuditTrail
{
    Task RecordMutationAsync(string actor, string action, string target, string details, CancellationToken cancellationToken = default);
}