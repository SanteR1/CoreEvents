using System.Text;
using System.Text.Json;
using Bookings.Infrastructure.Data;
using Bookings.Infrastructure.Data.Entities;
using Bookings.IntegrationTests.Infrastructure.Bases;
using Bookings.IntegrationTests.Infrastructure.Factories;
using Confluent.Kafka;
using CoreEvents.Shared.Contracts.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bookings.IntegrationTests.BackgroundServices;

public class KafkaConsumerBackgroundServiceTests(IntegrationTestFactory factory) : SharedIntegrationTestBase(factory)
{
    private readonly IntegrationTestFactory _factory = factory;

    [Fact]
    public async Task Consumer_ShouldProcessEventBookingValidationCompleted_AndSaveToInbox()
    {
        // Arrange
        var producerConfig = new ProducerConfig { BootstrapServers = _factory.ConnectionStringKafka };
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var causationId = Guid.NewGuid();

        var payload = JsonSerializer.Serialize(new
        {
            BookingId = bookingId,
            EventId = eventId,
            CanBeBooked = true
        });

        var headers = new Headers
        {
            { "messageId", Encoding.UTF8.GetBytes(messageId.ToString()) },
            { "correlationId", Encoding.UTF8.GetBytes(correlationId.ToString()) },
            { "createdAt", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")) },
            { "causationId", Encoding.UTF8.GetBytes(causationId.ToString()) },
            { "eventType", Encoding.UTF8.GetBytes("EventBookingValidationCompleted") }
        };

        var message = new Message<string, string>
        {
            Key = eventId.ToString(),
            Value = payload,
            Headers = headers
        };

        // Act
        await producer.ProduceAsync(KafkaTopics.EventConfirmed, message, TestContext.Current.CancellationToken);
        producer.Flush(TestContext.Current.CancellationToken);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();

        bool isProcessed = false;
        InboxMessage? inboxMessage = null;

        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(1000, TestContext.Current.CancellationToken);

            inboxMessage = await dbContext.InboxMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == messageId, TestContext.Current.CancellationToken);

            if (inboxMessage != null)
            {
                isProcessed = true;
                break;
            }
        }

        isProcessed.Should().BeTrue("Consumer должен был вычитать сообщение и сохранить его в InboxMessages в рамках транзакции");
        inboxMessage.Should().NotBeNull();

        inboxMessage!.CorrelationId.Should().Be(correlationId);
        inboxMessage.CausationId.Should().Be(causationId);
        inboxMessage.MessageType.Should().Be("EventBookingValidationCompleted");
        inboxMessage.Topic.Should().Be(KafkaTopics.EventConfirmed);
        inboxMessage.MessageKey.Should().Be(eventId.ToString());

        var expectedDict = JsonSerializer.Deserialize<EventBookingValidationCompleted>(payload);
        var actualDict = JsonSerializer.Deserialize<EventBookingValidationCompleted>(inboxMessage.Payload);
        actualDict.Should().BeEquivalentTo(expectedDict);

        inboxMessage.ProcessedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }
}
