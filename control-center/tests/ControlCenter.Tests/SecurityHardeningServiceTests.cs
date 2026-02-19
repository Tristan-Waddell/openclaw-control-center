using System.Security.Cryptography;
using System.Text;
using ControlCenter.Application.Abstractions;
using ControlCenter.Application.Services;

namespace ControlCenter.Tests;

public sealed class SecurityHardeningServiceTests
{
    [Fact]
    public async Task RequireSensitiveActionReauthAsync_ReturnsTrue_WhenSecretMatches()
    {
        var secrets = new InMemorySecretStore();
        await secrets.StoreAsync("reauth", "1234");
        var audit = new InMemoryAuditTrail();
        var service = new SecurityHardeningService(audit, secrets);

        var ok = await service.RequireSensitiveActionReauthAsync("reauth", "1234");

        Assert.True(ok);
        Assert.Single(audit.Rows);
    }

    [Fact]
    public void VerifySignedUpdate_ReturnsTrue_ForValidSignature()
    {
        using var rsa = RSA.Create(2048);
        var payload = Encoding.UTF8.GetBytes("control-center-update");
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var service = new SecurityHardeningService(new InMemoryAuditTrail(), new InMemorySecretStore());

        var ok = service.VerifySignedUpdate(payload, signature, rsa.ExportRSAPublicKeyPem());

        Assert.True(ok);
    }

    [Fact]
    public async Task RecordMutationAuditAsync_RedactsBeforePersist()
    {
        var audit = new InMemoryAuditTrail();
        var service = new SecurityHardeningService(audit, new InMemorySecretStore());

        await service.RecordMutationAuditAsync("ops", "update-config", "gateway", "token=abc123");

        Assert.Single(audit.Rows);
        Assert.DoesNotContain("abc123", audit.Rows[0]);
        Assert.Contains("token=***", audit.Rows[0]);
    }

    [Fact]
    public void RedactSecrets_MasksCommonPairs()
    {
        var service = new SecurityHardeningService(new InMemoryAuditTrail(), new InMemorySecretStore());
        var redacted = service.RedactSecrets("token=abc password=def secret=ghi");
        Assert.DoesNotContain("abc", redacted);
        Assert.DoesNotContain("def", redacted);
        Assert.DoesNotContain("ghi", redacted);
    }

    private sealed class InMemorySecretStore : ISecretStore
    {
        private readonly Dictionary<string, string> _secrets = [];

        public Task StoreAsync(string key, string secret, CancellationToken cancellationToken = default)
        {
            _secrets[key] = secret;
            return Task.CompletedTask;
        }

        public Task<string?> RetrieveAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(_secrets.TryGetValue(key, out var value) ? value : null);

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            _secrets.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAuditTrail : IAuditTrail
    {
        public List<string> Rows { get; } = [];

        public Task RecordMutationAsync(string actor, string action, string target, string details, CancellationToken cancellationToken = default)
        {
            Rows.Add($"{actor}:{action}:{target}:{details}");
            return Task.CompletedTask;
        }
    }
}