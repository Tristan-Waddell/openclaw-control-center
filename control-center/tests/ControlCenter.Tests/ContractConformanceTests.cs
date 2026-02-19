using System.Text.Json;
using ControlCenter.Contracts;

namespace ControlCenter.Tests;

public sealed class ContractConformanceTests
{
    [Fact]
    public void GatewayErrorDto_RoundTrips_WithRequiredFields()
    {
        var dto = new GatewayErrorDto("conflict", "Already exists", "Details", Retryable: false, "corr-1");
        var json = JsonSerializer.Serialize(dto);
        var parsed = JsonSerializer.Deserialize<GatewayErrorDto>(json);

        Assert.NotNull(parsed);
        Assert.Equal("conflict", parsed!.Code);
        Assert.Equal("corr-1", parsed.CorrelationId);
    }

    [Fact]
    public void RealtimeEnvelope_UsesSupportedVersion()
    {
        var envelope = new RealtimeEventEnvelopeDto("evt-1", "agent.updated", DateTimeOffset.UtcNow, 1, "{}");
        Assert.InRange(envelope.Version, 1, 1);
    }
}
