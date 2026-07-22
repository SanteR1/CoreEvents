using CoreEvents.Application.Interfaces.Repositories;
using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Enums;
using CoreEvents.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreEvents.IntegrationTests.Infrastructure.FaultInjection
{
    sealed class FaultInjectingBookingRepository(IBookingRepository inner, FaultInjectionState state, AppDbContext dbContext) : IBookingRepository
    {
        public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await CheckForTimeoutsAsync(cancellationToken);
            return await inner.GetByIdAsync(id, cancellationToken);
        }

        public async Task<int> GetBookingCountForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await CheckForTimeoutsAsync(cancellationToken);
            return await inner.GetBookingCountForUserAsync(userId, cancellationToken);
        }

        public async Task<IReadOnlyList<Guid>> GetPendingAsync(CancellationToken ct = default)
        {
            await CheckForTimeoutsAsync(ct);
            return await inner.GetPendingAsync(ct);
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            await CheckForTimeoutsAsync(ct);

            dbContext.ChangeTracker.DetectChanges();

            var modifiedBookings = dbContext.ChangeTracker.Entries<Booking>()
                .Where(e => e.State == EntityState.Modified)
                .Select(e => e.Entity)
                .ToList();

            bool shouldSimulateFailure = false;

            foreach (var booking in modifiedBookings)
            {
                // Пропускаем проверку, если это успешный откат (Reject)
                if (booking.Status == BookingStatus.Rejected)
                    continue;

                // Проверка 1: Падение по EventId
                if (booking.EventId == state.TargetEventIdForFailures)
                {
                    shouldSimulateFailure = true;
                    break;
                }

                // Проверка 2: Падение по конкретному BookingId
                if (state.ShouldFailForBooking(booking.Id))
                {
                    shouldSimulateFailure = true;
                    break;
                }
            }

            if (shouldSimulateFailure)
            {
                // Кастомная ошибка (если тест хочет выбросить что-то специфичное)
                if (state.CustomExceptionToThrow != null)
                {
                    throw state.CustomExceptionToThrow;
                }

                // Ошибка конкурентного доступа (полезно, если используете RowVersion в EF Core)
                if (state.SimulateConcurrencyException)
                {
                    throw new DbUpdateConcurrencyException("Simulated optimistic concurrency exception.");
                }

                // Классическая ошибка (по умолчанию)
                throw new DbUpdateException("Simulated transient database failure for concurrent rollback test.");
            }

            return await inner.SaveChangesAsync(ct);
        }

        public void Add(Booking booking)
        {
            inner.Add(booking);
        }

        public void Delete(Booking booking)
        {
            inner.Delete(booking);
        }

        public void Update(Booking booking)
        {
            inner.Update(booking);
        }

        // Вспомогательный метод для имитации зависания БД
        private async Task CheckForTimeoutsAsync(CancellationToken ct)
        {
            if (state.SimulateDatabaseTimeout)
            {
                // Task.Delay сработает и выбросит TaskCanceledException, 
                // если входящий CancellationToken истечет раньше, чем DatabaseDelay
                await Task.Delay(state.DatabaseDelay, ct);
            }
        }
    }
}
