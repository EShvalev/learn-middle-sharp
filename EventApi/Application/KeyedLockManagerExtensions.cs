namespace EventApi.Application
{
    static public class KeyedLockManagerExtensions
    {
        public static Task<IDisposable> AcquireEventLockAsync(this IKeyedLockManager manager, Guid eventId, CancellationToken ct = default)
            => manager.AcquireLockAsync($"Event:{eventId}", ct);
    }
}
