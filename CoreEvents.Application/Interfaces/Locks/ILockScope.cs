using System;
using System.Collections.Generic;
using System.Text;

namespace CoreEvents.Application.Interfaces.Locks
{
    public interface ILockScope : IAsyncDisposable
    {
        Task CompleteAsync(CancellationToken ct = default);
    }
}
