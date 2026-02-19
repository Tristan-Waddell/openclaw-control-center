using ControlCenter.App.Configuration;
using ControlCenter.Application.Services;
using ControlCenter.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControlCenter.App.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddControlCenter(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EnvironmentProfile>()
            .Bind(configuration.GetSection(EnvironmentProfile.SectionName))
            .ValidateDataAnnotations();

        var profile = new EnvironmentProfile();
        configuration.GetSection(EnvironmentProfile.SectionName).Bind(profile);

        services.AddInfrastructure(
            profile.SqliteConnectionString,
            profile.GatewayBaseUrl,
            profile.RealtimeWebSocketUrl,
            profile.RealtimeSseUrl,
            profile.SecretStoreScope);

        services.AddScoped<HealthService>();
        services.AddScoped<RealtimeSyncService>();
        return services;
    }
}
