using CoreEvents.Application.DTOs;
using CoreEvents.Application.Interfaces.Repositories;
using CoreEvents.Domain.Entities;
using CoreEvents.Infrastructure.Data;
using CoreEvents.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using FluentAssertions;

namespace CoreEvents.IntegrationTests.Repositories
{
    public class EventRepositoryTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
    {
        [Fact]
        public async Task AddAndSave_ViaRepository_ShouldPersistEvent()
        {
            // Arrange
            var newEvent = Event.Create("Real Test", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(1), 10);

            // Act
            await ExecuteScopeAsync(sp =>
            {
                var repo = sp.GetRequiredService<IEventRepository>();
                repo.Add(newEvent);
                return repo.SaveChangesAsync();
            });

            // Assert
            await ExecuteDbContextAsync(async ctx =>
            {
                var exists = await ctx.Events.AnyAsync(e => e.Id == newEvent.Id);
                exists.Should().BeTrue();
            });
        }

        [Fact]
        public async Task DatabaseCheckViolation_ShouldRejectEvent_WhenEndIsBeforeStart_ViaRawSql()
        {
            await ExecuteDbContextAsync(async db =>
            {
                // Arrange
                var eventId = Guid.NewGuid();
                var startAt = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-dd HH:mm:ssZ");
                var endAt = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd HH:mm:ssZ");
                int totalSeats = 10;
                int availableSeats = 10;

                var sql = $@"
                    INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"",""available_seats"")
                    VALUES ('{eventId}', 'Тест SQL', '{startAt}', '{endAt}', {totalSeats},'{availableSeats}');";

                // Act
                Func<Task> action = async () => await db.Database.ExecuteSqlRawAsync(sql, TestContext.Current.CancellationToken);

                // Assert
                var exceptionAssertion = await action.Should().ThrowAsync<PostgresException>();

                exceptionAssertion.Which.SqlState.Should().Be("23514"); // 23514 - код ошибки check_violation
                exceptionAssertion.Which.MessageText.Should().Contain("CK_events_dates");
            });
        }

        [Fact]
        public async Task Delete_ShouldCascadeDeleteOrHandleDependentBookingsCorrectly()
        {
            // Arrange
            var eventId = await ExecuteDbContextAsync(async ctx =>
            {
                var e = Event.Create("Cascade Test", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 5);
                var b = Booking.Create(e.Id);
                ctx.Events.Add(e);
                ctx.Bookings.Add(b);
                await ctx.SaveChangesAsync();
                return e.Id;
            });

            // Act
            await ExecuteScopeAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IEventRepository>();
                var eventToDelete = await repo.GetByIdAsync(eventId, CancellationToken.None);

                if (eventToDelete is not null)
                {
                    repo.Delete(eventToDelete);
                    await repo.SaveChangesAsync(CancellationToken.None);
                }
            });

            // Assert
            await ExecuteDbContextAsync(async ctx =>
            {
                var relatedBookingsCount = await ctx.Bookings.CountAsync(b => b.EventId == eventId);
                relatedBookingsCount.Should().Be(0);
            });
        }

        [Fact]
        public async Task Delete_ShouldRemoveEventAndReturnExpectedState()
        {
            // Arrange
            var id = await ExecuteDbContextAsync(async ctx =>
            {
                var e = Event.Create("To Delete", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
                ctx.Events.Add(e);
                await ctx.SaveChangesAsync();
                return e.Id;
            });

            // Act
            await ExecuteScopeAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IEventRepository>();
                var existing = await repo.GetByIdAsync(id, CancellationToken.None);
                repo.Delete(existing!);
                await repo.SaveChangesAsync(CancellationToken.None);
            });

            // Assert
            await ExecuteDbContextAsync(async ctx =>
            {
                var exists = await ctx.Events.AnyAsync(e => e.Id == id);
                exists.Should().BeFalse();
            });
        }

        [Fact]
        public async Task GetAllAsync_AsNoTracking_ShouldReturnDetachedEntitiesNotTrackedByContext()
        {
            // Arrange
            await ExecuteDbContextAsync(async ctx =>
            {
                var e = Event.Create("NoTrack", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
                ctx.Events.Add(e);
                await ctx.SaveChangesAsync();
            });

            // Act & Assert
            await ExecuteScopeAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IEventRepository>();
                var context = sp.GetRequiredService<AppDbContext>();
                var entities = await repo.GetAllAsync(new EventFilter(), CancellationToken.None);

                entities.Items.Select(e => context.Entry(e).State)
                    .Should().OnlyContain(state => state == EntityState.Detached);
            });
        }

        [Fact]
        public async Task GetAllAsync_WithFilter_ShouldReturnFilteredEvents()
        {
            // Arrange
            var fromDate = DateTime.UtcNow.AddDays(1);
            var toDate = DateTime.UtcNow.AddDays(1).AddHours(1);

            var filter = new EventFilter() { Title = "ven", From = fromDate, To = toDate.AddSeconds(+1), PageSize = 10, Page = 1 };
            await ExecuteDbContextAsync(async ctx =>
            {

                List<Event> events = new List<Event>(20);
                for (int i = 0; i < 20; i++)
                {
                    events.Add(Event.Create("Event " + i, fromDate, toDate, 10));
                }
                ctx.Events.AddRange(events);
                await ctx.SaveChangesAsync();
            });

            // Act
            var result = await ExecuteScopeAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IEventRepository>();

                return await repo.GetAllAsync(filter, CancellationToken.None);
            });

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().NotBeNull();
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(20);
            result.Items.Should().AllSatisfy(e =>
            {
                e.Title.Should().Contain("ven");
                e.AvailableSeats.Should().Be(10);
                e.TotalSeats.Should().Be(10);
            });

        }

        [Fact]
        public async Task GetAllAsync_ValidPageAndPageSize_ReturnsCorrectPageAndItemsCount()
        {
            // Arrange
            var fromDate = DateTime.UtcNow.AddDays(1);
            var toDate = DateTime.UtcNow.AddDays(1).AddHours(1);

            var filter = new EventFilter() { PageSize = 10, Page = 3 };
            await ExecuteDbContextAsync(async ctx =>
            {
                List<Event> events = new List<Event>(27);
                for (int i = 0; i < 27; i++)
                {
                    events.Add(Event.Create("Event " + i, fromDate, toDate, 10));
                }
                ctx.Events.AddRange(events);
                await ctx.SaveChangesAsync();
            });

            // Act
            var result = await ExecuteScopeAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IEventRepository>();

                return await repo.GetAllAsync(filter, CancellationToken.None);
            });

            // Assert
            result.Should().NotBeNull();
            result.CurrentPage.Should().Be(3);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(27);
            result.Items.Should().HaveCount(7);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldRetrieveEvent()
        {
            // Arrange
            var id = await ExecuteDbContextAsync(async ctx =>
            {
                var e = Event.Create("Event Test", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
                ctx.Events.Add(e);
                await ctx.SaveChangesAsync();
                return e.Id;
            });

            // Act
            var result = await ExecuteScopeAsync(sp => sp.GetRequiredService<IEventRepository>()
                .GetByIdAsync(id, CancellationToken.None));

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(id);
        }

        [Fact]
        public async Task GetByIdAsync_WhenEventDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await ExecuteScopeAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IEventRepository>();
                return await repo.GetByIdAsync(nonExistentId);
            });

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task LoadEventWithBookings_ReturnsCorrectCount()
        {
            // Arrange
            var eventId = await ExecuteDbContextAsync(async ctx =>
            {
                var eventId = Event.Create("Event 1 for Include", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
                ctx.Events.Add(eventId);
                var booking1 = Booking.Create(eventId.Id);
                var booking2 = Booking.Create(eventId.Id);
                ctx.Bookings.AddRange(booking1, booking2);
                await ctx.SaveChangesAsync();
                return eventId.Id;
            });

            // Act
            var result = await ExecuteDbContextAsync(ctx =>
            {
                return ctx.Events
                    .Include(a => a.Bookings)
                    .FirstAsync(a => a.Id == eventId);
            });

            // Assert
            result.Should().NotBeNull();
            result.Bookings.Should().HaveCount(2);
        }

        [Fact]
        public async Task Update_ShouldPersistChangesAndReturnUpdatedEntity()
        {
            // Arrange
            var dateStartAtUpdate = DateTime.UtcNow.AddDays(1);
            var dateEndAtUpdate = DateTime.UtcNow.AddDays(2);
            var id = await ExecuteDbContextAsync(async ctx =>
            {
                var e = Event.Create("Old Title", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
                ctx.Events.Add(e);
                await ctx.SaveChangesAsync();
                return e.Id;
            });

            // Act
            await ExecuteScopeAsync(async sp =>
            {
                var repo = sp.GetRequiredService<IEventRepository>();
                var existing = await repo.GetByIdAsync(id, CancellationToken.None);

                existing?.Update(
                    title: "Updated Title",
                    startAt: dateStartAtUpdate,
                    endAt: dateEndAtUpdate,
                    description: "Updated description"
                    );

                repo.Update(existing!);
                await repo.SaveChangesAsync(CancellationToken.None);
                return existing;
            });

            // Assert
            var updateEvent = await ExecuteDbContextAsync(async ctx => await ctx.Events.FindAsync(id));
            updateEvent.Should().NotBeNull();
            updateEvent.Id.Should().Be(id);
            updateEvent.Title.Should().Contain("Updated Title");
            updateEvent.Description.Should().Contain("Updated description");
            updateEvent.StartAt.Should().BeCloseTo(dateStartAtUpdate, TimeSpan.FromMilliseconds(1));
            updateEvent.EndAt.Should().BeCloseTo(dateEndAtUpdate, TimeSpan.FromMilliseconds(1));
        }
    }
}
