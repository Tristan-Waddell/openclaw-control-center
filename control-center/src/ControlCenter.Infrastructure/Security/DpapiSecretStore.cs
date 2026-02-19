using System.Security.Cryptography;
using System.Text;
using ControlCenter.Application.Abstractions;

namespace ControlCenter.Infrastructure.Security;

public sealed class DpapiSecretStore : ISecretStore
{
    private readonly string _scope;
    private readonly Dictionary<string, string> _memoryFallback = new(StringComparer.Ordinal);

    public DpapiSecretStore(string scope)
    {
        _scope = scope;
    }

    public Task StoreAsync(string key, string secret, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scopedKey = BuildScopedKey(key);

        if (OperatingSystem.IsWindows())
        {
            var plain = Encoding.UTF8.GetBytes(secret);
            var encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            _memoryFallback[scopedKey] = Convert.ToBase64String(encrypted);
        }
        else
        {
            _memoryFallback[scopedKey] = secret;
        }

        return Task.CompletedTask;
    }

    public Task<string?> RetrieveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scopedKey = BuildScopedKey(key);

        if (!_memoryFallback.TryGetValue(scopedKey, out var stored))
        {
            return Task.FromResult<string?>(null);
        }

        if (OperatingSystem.IsWindows())
        {
            var encrypted = Convert.FromBase64String(stored);
            var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Task.FromResult<string?>(Encoding.UTF8.GetString(plain));
        }

        return Task.FromResult<string?>(stored);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _memoryFallback.Remove(BuildScopedKey(key));
        return Task.CompletedTask;
    }

    private string BuildScopedKey(string key) => $"{_scope}:{key}";
}
