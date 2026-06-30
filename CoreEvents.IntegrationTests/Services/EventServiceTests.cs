using CoreEvents.Application.DTOs;
using CoreEvents.Application.Services;
using CoreEvents.Domain.Entities;
using CoreEvents.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEvents.IntegrationTests.Services
{
    public class EventServiceTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
    {
        [Fact]
        public async Task GetAllEventsAsync_WithLargeDataset_ShouldExecuteEfficientSkipTakeWithoutMemoryOverflow()
        {
            // Arrange
            await ExecuteDbContextAsync(async ctx =>
            {
                var events = new List<Event>(1000);
                for (int i = 0; i < 1000; i++)
                {
                    var futureDate1 = DateTime.UtcNow.AddDays(1);
                    var futureDate2 = futureDate1.AddHours(1);

                    events.Add(Event.Create($"Large {i}", futureDate1, futureDate2, 5));
                }
                await ctx.AddRangeAsync(events);
                await ctx.SaveChangesAsync();
            });

            // Act
            var result = await ExecuteScopeAsync(sp =>
            {
                var service = sp.GetRequiredService<IEventService>();
                return service.GetAllEventsAsync(new EventFilter { Page = 1, PageSize = 10000 }, CancellationToken.None);
            });

            // Assert
            result.TotalCount.Should().Be(1000);
            result.Items.Should().HaveCount(100);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(100);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithLastPage_ShouldReturnPartialResultsCorrectly()
        {
            // Arrange
            await ExecuteDbContextAsync(async ctx =>
            {
                var events = new List<Event>();

                for (int i = 0; i < 13; i++)
                {
                    var futureDate1 = DateTime.UtcNow;
                    var futureDate2 = DateTime.UtcNow.AddHours(1);

                    events.Add(Event.Create($"Event {i}", futureDate1, futureDate2, 5));
                }
                await ctx.AddRangeAsync(events);
                await ctx.SaveChangesAsync();
            });

            // Act
            var result = await ExecuteScopeAsync(sp =>
            {
                var service = sp.GetRequiredService<IEventService>();
                return service.GetAllEventsAsync(new EventFilter { Page = 2, PageSize = 10 }, CancellationToken.None);
            });

            // Assert
            result.TotalCount.Should().Be(13);
            result.TotalPages.Should().Be(2);
            result.Items.Count.Should().Be(3);
        }


        [Fact]
        public async Task GetAllAsync_WithNoMatchingEvents_ShouldReturnEmptyCollectionAndZeroTotalCount()
        {
            // Act
            var result = await ExecuteScopeAsync(sp =>
            {
                var service = sp.GetRequiredService<IEventService>();
                return service.GetAllEventsAsync(new EventFilter { Title = "NonExistent" }, CancellationToken.None);
            });

            // Assert
            result.TotalCount.Should().Be(0);
            result.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_WithPagination_ShouldReturnCorrectPageItemsAndTotalCount()
        {
            // Arrange
            await ExecuteDbContextAsync(async ctx =>
            {
                var events = new List<Event>();

                for (int i = 0; i < 25; i++)
                {
                    var futureDate1 = DateTime.UtcNow.AddDays(i);
                    var futureDate2 = DateTime.UtcNow.AddDays(i + 1);

                    events.Add(Event.Create($"Event {i}", futureDate1, futureDate2, 5));
                }
                await ctx.AddRangeAsync(events);
                await ctx.SaveChangesAsync();
            });

            // Act
            var result = await ExecuteScopeAsync(sp =>
            {
                var service = sp.GetRequiredService<IEventService>();
                return service.GetAllEventsAsync(new EventFilter { Page = 3, PageSize = 10 }, CancellationToken.None);
            });

            // Assert
            result.TotalCount.Should().Be(25);
            result.TotalPages.Should().Be(3);
            result.Items.Should().HaveCount(5);
            result.CurrentPage.Should().Be(3);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithCombinedFilters_ShouldApplyAllConditionsWithAnd()
        {
            // Arrange
            var dateStartAt = DateTime.UtcNow.AddDays(1);
            var dateEndAt = dateStartAt.AddHours(5);
            await ExecuteDbContextAsync(ctx =>
            {
                var event1 = Event.Create("Target Event", dateStartAt, dateEndAt, 10);
                ctx.Add(event1);
                return ctx.SaveChangesAsync();
            });

            // Act
            var result = await ExecuteScopeAsync(sp =>
            {
                var service = sp.GetRequiredService<IEventService>();
                return service.GetAllEventsAsync(
                    new EventFilter { Title = "Target", From = dateStartAt, To = dateStartAt.AddHours(10) }, CancellationToken.None);
            });

            // Assert
            result.Items.Should().ContainSingle();
            result.Items.Should().AllSatisfy(e =>
            {
                e.Title.Should().Be("Target Event");
                e.AvailableSeats.Should().Be(10);
                e.TotalSeats.Should().Be(10);
                e.StartAt.Should().BeCloseTo(dateStartAt, TimeSpan.FromMilliseconds(1));
                e.EndAt.Should().BeCloseTo(dateEndAt, TimeSpan.FromMilliseconds(1));
            });
        }

        [Fact]
        public async Task GetAllEventsAsync_WithFromAndToFilter_ShouldApplyInclusiveExclusiveBoundsCorrectly()
        {
            // Arrange
            var dateStartAt = DateTime.UtcNow.AddDays(1);
            var dateEndAt = dateStartAt.AddHours(2);
            await ExecuteDbContextAsync(ctx =>
            {
                var event1 = Event.Create("Exact Match", dateStartAt, dateEndAt, 5);
                var event2 = Event.Create("Exact Match", dateStartAt.AddDays(1), dateEndAt.AddDays(1).AddHours(2), 5);
                ctx.AddRange(event1, event2);
                return ctx.SaveChangesAsync();
            });

            // Act
            var result = await ExecuteScopeAsync(sp =>
            {
                var service = sp.GetRequiredService<IEventService>();
                return service.GetAllEventsAsync(new EventFilter { From = dateStartAt, To = dateEndAt }, CancellationToken.None);
            });

            // Assert
            result.Items.Should().ContainSingle();
            result.Items.Should().AllSatisfy(e =>
            {
                e.Title.Should().Be("Exact Match");
                e.AvailableSeats.Should().Be(5);
                e.TotalSeats.Should().Be(5);
                e.StartAt.Should().BeCloseTo(dateStartAt, TimeSpan.FromMilliseconds(1));
                e.EndAt.Should().BeCloseTo(dateEndAt, TimeSpan.FromMilliseconds(1));
            });
        }

        [Fact]
        public async Task GetAllEventsAsync_WithFromFilter_ShouldIncludeEventsStartingAtOrAfterFrom()
        {
            // Arrange
            var futureDate1 = DateTime.UtcNow.AddDays(1);
            var futureDate2 = DateTime.UtcNow.AddDays(2);
            var filterDate = futureDate1.AddHours(1);
            await ExecuteDbContextAsync(ctx =>
            {
                var event1 = Event.Create("Before", futureDate1, futureDate1.AddHours(2), 5);
                var event2 = Event.Create("AtFrom", futureDate2, futureDate2.AddHours(2), 5);
                ctx.AddRange(event1, event2);
                return ctx.SaveChangesAsync();
            });

            // Act
            var result = await ExecuteScopeAsync(sp =>
            {
                var service = sp.GetRequiredService<IEventService>();
                return service.GetAllEventsAsync(new EventFilter { From = filterDate }, CancellationToken.None);
            });

            // Assert
            result.Items.Should().ContainSingle();
            result.TotalCount.Should().Be(1);
            result.Items.Should().AllSatisfy(e =>
            {
                e.Title.Should().Be("AtFrom");
                e.AvailableSeats.Should().Be(5);
                e.TotalSeats.Should().Be(5);
                e.StartAt.Should().BeCloseTo(futureDate2, TimeSpan.FromMilliseconds(1));
                e.EndAt.Should().BeCloseTo(futureDate2.AddHours(2), TimeSpan.FromMilliseconds(1));
            });
        }

        [Fact]
        public async Task GetAllEventsAsync_WithTitleFilter_ShouldUseILikeAndMatchCaseInsensitively()
        {
            // Arrange
            await ExecuteDbContextAsync(ctx =>
            {
                var event1 = Event.Create("TEST Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
                var event2 = Event.Create("Event A", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
                ctx.AddRange(event1, event2);
                return ctx.SaveChangesAsync();
            });
            // Act
            var result = await ExecuteScopeAsync(sp =>
            {
                var service = sp.GetRequiredService<IEventService>();
                return service.GetAllEventsAsync(new EventFilter { Title = "test" }, CancellationToken.None);
            });

            // Assert
            result.Items.Should().ContainSingle();
            result.TotalCount.Should().Be(1);
            result.Items.Should().AllSatisfy(e =>
            {
                e.Title.Should().Be("TEST Event");
                e.AvailableSeats.Should().Be(10);
                e.TotalSeats.Should().Be(10);
            });
        }

        [Fact]
        public async Task GetAllEventsAsync_WithTitleNullFilter_ShouldReturnAllEvents()
        {
            // Arrange
            await ExecuteDbContextAsync(ctx =>
            {
                var event1 = Event.Create("Event A", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
                var event2 = Event.Create("Event B", DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(4), 10);
                ctx.AddRange(event1, event2);
                return ctx.SaveChangesAsync();
            });

            // Act
            var result = await ExecuteScopeAsync(sp =>
            {
                var service = sp.GetRequiredService<IEventService>();
                return service.GetAllEventsAsync(new EventFilter { Title = null! }, CancellationToken.None);
            });

            // Assert
            result.TotalCount.Should().Be(2);
            result.Items.Count.Should().Be(2);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithToFilter_ShouldExcludeEventsEndingAtOrAfterTo()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var filterDate = futureDate.AddHours(2);
            await ExecuteDbContextAsync(ctx =>
            {
                var event1 = Event.Create("Inside", futureDate, filterDate, 5);
                var event2 = Event.Create("Outside", futureDate.AddHours(1), futureDate.AddHours(3), 5);
                ctx.AddRange(event1, event2);
                return ctx.SaveChangesAsync();
            });

            // Act
            var result = await ExecuteScopeAsync(sp =>
            {
                var service = sp.GetRequiredService<IEventService>();
                return service.GetAllEventsAsync(new EventFilter { To = filterDate }, CancellationToken.None);
            });

            // Assert
            result.Items.Should().ContainSingle();
            result.Items.Should().AllSatisfy(e =>
            {
                e.Title.Should().Be("Inside");
            });
        }
    }
}
