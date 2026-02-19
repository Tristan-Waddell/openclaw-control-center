using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ControlCenter.Application.Services;

public sealed class ReliabilityService
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _idempotencyKeys = new(StringComparer.Ordinal);
    private int _consecutiveFailures;

    public bool TryRegisterIdempotencyKey(string key)
        => _idempotencyKeys.TryAdd(key, DateTimeOffset.UtcNow);

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxAttempts = 3, CancellationToken cancellationToken = default)
    {
        if (_consecutiveFailures >= maxAttempts)
        {
            throw new InvalidOperationException("Circuit open due to repeated failures.");
        }

        Exception? last = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = await operation();
                _consecutiveFailures = 0;
                return result;
            }
            catch (Exception ex)
            {
                last = ex;
                _consecutiveFailures++;
                if (attempt == maxAttempts)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken);
            }
        }

        throw last ?? new InvalidOperationException("Retry failed without exception.");
    }

    public string ComputeCacheIntegrityHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }
}