namespace CoreEvents.Application.Interfaces.Locks
{
    public interface ILockProvider
    {
        Task<ILockScope> AcquireLockAsync(string resourceKey, CancellationToken ct = default);
    }
}
