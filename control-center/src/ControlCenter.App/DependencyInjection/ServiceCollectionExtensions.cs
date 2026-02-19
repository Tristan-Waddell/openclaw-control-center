using ControlCenter.App.Configuration;
using ControlCenter.Application.Abstractions;
using ControlCenter.Application.Services;
using ControlCenter.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ControlCenter.App.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddControlCenter(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EnvironmentProfile>()
            .Bind(configuration.GetSection(EnvironmentProfile.SectionName))
            .ValidateDataAnnotations();

        services.AddSingleton<IHealthRepository>(provider =>
        {
            var profile = provider.GetRequiredService<IOptions<EnvironmentProfile>>().Value;
            return new SqliteHealthRepository(profile.SqliteConnectionString);
        });

        services.AddScoped<HealthService>();
        return services;
    }
}
