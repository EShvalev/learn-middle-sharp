using System.Collections.Concurrent;

namespace EventApi.Application
{
    public class KeyedLockManager : IKeyedLockManager
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public async Task<IDisposable> AcquireLockAsync(string key, CancellationToken ct = default)
        {
            var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(ct);
            return new Releaser(semaphore);
        }

        private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
        {
            public void Dispose() => semaphore.Release();
        }
    }
}
