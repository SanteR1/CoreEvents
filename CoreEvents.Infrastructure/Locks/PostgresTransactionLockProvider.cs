using System.Security.Cryptography;
using System.Text;
using CoreEvents.Application.Interfaces.Locks;
using CoreEvents.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreEvents.Infrastructure.Locks
{
    internal sealed class PostgresTransactionLockProvider : ILockProvider
    {
        private readonly AppDbContext _dbContext;

        public PostgresTransactionLockProvider(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ILockScope> AcquireLockAsync(string resourceKey, CancellationToken ct = default)
        {
            var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                long lockId = GenerateLockId(resourceKey);

                await _dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_xact_lock({0});",
                    [lockId],
                    cancellationToken: ct);

                return new PostgresTransactionLockScope(transaction);
            }
            catch
            {
                await transaction.DisposeAsync();
                throw;
            }
        }

        public async Task<ILockScope?> TryAcquireLockAsync(string resourceKey, CancellationToken ct = default)
        {
            var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

            try
            {
                long lockId = GenerateLockId(resourceKey);

                bool lockAcquired = await _dbContext.Database
                    .SqlQueryRaw<bool>("SELECT pg_try_advisory_xact_lock({0}) AS \"Value\"", lockId)
                    .SingleAsync(ct);

                if (lockAcquired)
                {
                    return new PostgresTransactionLockScope(transaction);
                }

                await transaction.DisposeAsync();
                return null;
            }
            catch
            {
                await transaction.DisposeAsync();
                throw;
            }
        }
        
        private static long GenerateLockId(string resourceKey)
        {
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(resourceKey));
            return BitConverter.ToInt64(hashBytes, 0);
        }
    }
}
