using ControlCenter.Application.Services;

namespace ControlCenter.Tests;

public sealed class ReliabilityServiceTests
{
    [Fact]
    public void EvaluateRuntimeMode_ReturnsExpectedMode()
    {
        var service = new ReliabilityService();

        Assert.Equal(RuntimeMode.Online, service.EvaluateRuntimeMode(isGatewayReachable: true, isRealtimeConnected: true));
        Assert.Equal(RuntimeMode.Degraded, service.EvaluateRuntimeMode(isGatewayReachable: true, isRealtimeConnected: false));
        Assert.Equal(RuntimeMode.Offline, service.EvaluateRuntimeMode(isGatewayReachable: false, isRealtimeConnected: false));
    }

    [Fact]
    public void TryRegisterIdempotencyKey_RejectsDuplicate()
    {
        var service = new ReliabilityService();
        Assert.True(service.TryRegisterIdempotencyKey("k1"));
        Assert.False(service.TryRegisterIdempotencyKey("k1"));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_RetriesAndSucceeds()
    {
        var service = new ReliabilityService();
        var count = 0;

        var value = await service.ExecuteWithRetryAsync(() =>
        {
            count++;
            if (count < 2)
            {
                throw new InvalidOperationException("fail once");
            }

            return Task.FromResult(42);
        });

        Assert.Equal(42, value);
        Assert.Equal(2, count);
    }

    [Fact]
    public void ValidateCacheIntegrity_ReturnsFalse_ForTamperedFile()
    {
        var service = new ReliabilityService();
        var path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "v1");
            var hash = service.ComputeCacheIntegrityHash(path);
            File.WriteAllText(path, "v2");

            Assert.False(service.ValidateCacheIntegrity(path, hash));
        }
        finally
        {
            File.Delete(path);
        }
    }
}