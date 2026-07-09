namespace CoreEvents.Application.Interfaces.Locks
{
    public interface ILockScope : IAsyncDisposable
    {
        Task CompleteAsync(CancellationToken ct = default);
    }
}
