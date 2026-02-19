using ControlCenter.Contracts;

namespace ControlCenter.Application.Abstractions;

public interface IGatewayApiClient
{
    Task<GatewayStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentSummaryDto>> GetAgentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectSummaryDto>> GetProjectsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskRunDto>> GetActiveRunsAsync(CancellationToken cancellationToken = default);
    Task<UsageSummaryDto> GetUsageSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CronJobDto>> GetCronJobsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConfigEntryDto>> GetConfigEntriesAsync(CancellationToken cancellationToken = default);
}
