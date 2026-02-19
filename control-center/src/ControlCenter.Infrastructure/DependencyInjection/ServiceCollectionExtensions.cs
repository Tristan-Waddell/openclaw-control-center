using ControlCenter.Application.Abstractions;
using ControlCenter.Infrastructure.Gateway;
using ControlCenter.Infrastructure.Persistence;
using ControlCenter.Infrastructure.Realtime;
using ControlCenter.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace ControlCenter.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string sqliteConnectionString,
        string gatewayBaseUrl,
        string realtimeWebSocketUrl,
        string realtimeSseUrl,
        string secretStoreScope)
    {
        services.AddHttpClient<IGatewayApiClient, HttpGatewayApiClient>(client =>
        {
            client.BaseAddress = new Uri(gatewayBaseUrl, UriKind.Absolute);
        });

        services.AddHttpClient("Realtime");
        services.AddSingleton<IRealtimeClient>(provider =>
        {
            var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("Realtime");
            return new GatewayRealtimeClient(
                httpClient,
                new Uri(realtimeWebSocketUrl, UriKind.Absolute),
                new Uri(realtimeSseUrl, UriKind.Absolute));
        });

        services.AddSingleton<IGatewayCache>(_ => new SqliteGatewayCache(sqliteConnectionString));
        services.AddSingleton<IEventJournal>(_ => new SqliteEventJournal(sqliteConnectionString));
        services.AddSingleton<IHealthRepository>(_ => new SqliteHealthRepository(sqliteConnectionString));
        services.AddSingleton<ISecretStore>(_ => new DpapiSecretStore(secretStoreScope));

        return services;
    }
}
