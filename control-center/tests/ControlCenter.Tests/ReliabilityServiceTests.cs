using ControlCenter.Application.Services;

namespace ControlCenter.Tests;

public sealed class ReliabilityServiceTests
{
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
}