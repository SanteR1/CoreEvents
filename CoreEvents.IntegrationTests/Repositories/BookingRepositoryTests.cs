using CoreEvents.Data.Repositories.Interfaces;
using CoreEvents.IntegrationTests.Infrastructure;
using CoreEvents.Models.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;

namespace CoreEvents.IntegrationTests.Repositories
{
    public class BookingRepositoryTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
    {
        [Fact]
        public async Task Add_ExistEventId_ShouldInsertBookingWithPendingStatus()
        {
            // Arrange
            var eventId = await ExecuteDbContextAsync(async ctx =>
            {
                var e = Event.Create("Booking Test", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
                ctx.Events.Add(e);
                await ctx.SaveChangesAsync();
                return e.Id;
            });
            var booking = Booking.Create(eventId);

            // Act
            await ExecuteScopeAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IBookingRepository>();
                repo.Add(booking);
                await repo.SaveChangesAsync();
            });

            // Assert
            await ExecuteDbContextAsync(async ctx =>
            {
                var exists = await ctx.Bookings.FindAsync(booking.Id);

                exists.Should().NotBeNull();
                booking.Id.Should().Be(exists.Id);
                eventId.Should().Be(exists.EventId);
            });
        }

        [Fact]
        public async Task Add_InvalidEventId_ShouldRespectForeignKeyConstraintAndThrowsDbUpdateException()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            var booking = Booking.Create(eventId);

            // Act & Assert
            await ExecuteScopeAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IBookingRepository>();

                Func<Task> action = async () =>
                {
                    repo.Add(booking);
                    await repo.SaveChangesAsync(TestContext.Current.CancellationToken);
                };

                await action.Should().ThrowAsync<DbUpdateException>().WithInnerException(typeof(Exception)).WithMessage("*23503*");
            });
        }

        [Fact]
        public async Task Delete_ShouldRemoveBooking()
        {
            // Arrange
            var bookingId = await ExecuteDbContextAsync(async ctx =>
            {
                var eventId = Event.Create("Delete Booking", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
                ctx.Events.Add(eventId);
                var b = Booking.Create(eventId.Id);
                ctx.Bookings.Add(b);
                await ctx.SaveChangesAsync();
                return b.Id;
            });

            // Act
            await ExecuteScopeAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IBookingRepository>();
                var booking = await repo.GetByIdAsync(bookingId, CancellationToken.None);
                repo.Delete(booking!);
                await repo.SaveChangesAsync(CancellationToken.None);
            });
            
            // Assert
            await ExecuteDbContextAsync(async ctx =>
            {
                var exists = await ctx.Bookings.AnyAsync(b => b.Id == bookingId);

                exists.Should().BeFalse();
            });
        }

        [Fact]
        public async Task GetByIdAsync_ExistEventId_ShouldRetrieveBookingByIdAndReturnEntity()
        {
            // Arrange
            var bookingId = await ExecuteDbContextAsync(async ctx =>
            {
                var eventId = Event.Create("Get By Id", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
                ctx.Events.Add(eventId);
                var b = Booking.Create(eventId.Id);
                ctx.Bookings.Add(b);
                await ctx.SaveChangesAsync();
                return b.Id;
            });

            // Act
            var result = await ExecuteScopeAsync(sp =>
                sp.GetRequiredService<IBookingRepository>()
                .GetByIdAsync(bookingId, CancellationToken.None));

            // Assert
            result.Should().NotBeNull();
            bookingId.Should().Be(result.Id);
        }

        [Fact]
        public async Task GetPendingAsync_ShouldReturnOnlyPendingBookingIds()
        {
            // Arrange
            await ExecuteDbContextAsync(async ctx =>
            {
                var eventId = Event.Create("Pending Filter", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
                ctx.Events.Add(eventId);
                var b1 = Booking.Create(eventId.Id);
                var b2 = Booking.Create(eventId.Id);
                ctx.Bookings.AddRange(b1,b2);
                await ctx.SaveChangesAsync();
            });

            // Act
            var pendingIds = await ExecuteScopeAsync(sp =>
                sp.GetRequiredService<IBookingRepository>()
                .GetPendingAsync(CancellationToken.None));

            // Assert
            pendingIds.Should().HaveCount(2);

        }

        [Fact]
        public async Task LoadBookingWithEvent_ReturnsCorrectEvent()
        {
            // Arrange
            var bookingId = await ExecuteDbContextAsync(async ctx =>
            {
                var eventId = Event.Create("Event 1 for Include", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
                ctx.Events.Add(eventId);
                var booking = Booking.Create(eventId.Id);
                ctx.Bookings.Add(booking);
                await ctx.SaveChangesAsync();
                return booking.Id;
            });

            // Act & Assert
            await ExecuteDbContextAsync(async ctx =>
            {
                var loadedBooking = await ctx.Bookings
                    .Include(b => b.Event)
                    .FirstAsync(b => b.Id == bookingId);

                loadedBooking.Event.Should().NotBeNull();
                loadedBooking.Event.Title.Should().Be("Event 1 for Include");
            });
        }

        [Fact]
        public async Task Update_ShouldPersistBookingStatusChange()
        {
            // Arrange
            var bookingId = await ExecuteDbContextAsync(async ctx =>
            {
                var eventId = Event.Create("Status Update", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
                ctx.Events.Add(eventId);
                var b = Booking.Create(eventId.Id);
                ctx.Bookings.Add(b);
                await ctx.SaveChangesAsync();
                return b.Id;
            });

            // Act
            await ExecuteScopeAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IBookingRepository>();
                var booking = await repo.GetByIdAsync(bookingId, CancellationToken.None);
                booking!.Confirm();
                repo.Update(booking);
                await repo.SaveChangesAsync(CancellationToken.None);
            });

            // Assert
            await ExecuteDbContextAsync(async ctx =>
            {
                var updated = await ctx.Bookings.FindAsync([bookingId], CancellationToken.None);
                updated.Should().NotBeNull();
                updated.Status.Should().Be(BookingStatus.Confirmed);
                updated.ProcessedAt.Should().NotBeNull();
            });
        }
    }
}
