namespace EventApi.Application
{
    public interface IKeyedLockManager
    {
        Task<IDisposable> AcquireLockAsync(string key, CancellationToken ct = default);
    }
}
