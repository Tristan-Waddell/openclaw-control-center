using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ControlCenter.Application.Abstractions;

namespace ControlCenter.Application.Services;

public sealed class SecurityHardeningService
{
    private readonly IAuditTrail _auditTrail;
    private readonly ISecretStore _secretStore;

    public SecurityHardeningService(IAuditTrail auditTrail, ISecretStore secretStore)
    {
        _auditTrail = auditTrail;
        _secretStore = secretStore;
    }

    public bool IsLeastPrivilegeRuntime()
    {
        var isWindows = OperatingSystem.IsWindows();
        var isServiceAccount = string.Equals(Environment.UserName, "SYSTEM", StringComparison.OrdinalIgnoreCase);
        return isWindows && !isServiceAccount;
    }

    public async Task<bool> RequireSensitiveActionReauthAsync(string expectedSecretName, string providedSecret, CancellationToken cancellationToken = default)
    {
        var stored = await _secretStore.RetrieveAsync(expectedSecretName, cancellationToken);
        var passed = string.Equals(stored, providedSecret, StringComparison.Ordinal);
        await _auditTrail.RecordMutationAsync("operator", "reauth", expectedSecretName, passed ? "passed" : "failed", cancellationToken);
        return passed;
    }

    public bool VerifySignedUpdate(byte[] payload, byte[] signature, string publicKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        return rsa.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public string RedactSecrets(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        return Regex.Replace(
            input,
            "(?i)\\b(token|password|secret)=([^\\s;,&]+)",
            "$1=***",
            RegexOptions.CultureInvariant);
    }

    public Task RecordMutationAuditAsync(string actor, string action, string target, string details, CancellationToken cancellationToken = default)
        => _auditTrail.RecordMutationAsync(actor, action, target, RedactSecrets(details), cancellationToken);
}