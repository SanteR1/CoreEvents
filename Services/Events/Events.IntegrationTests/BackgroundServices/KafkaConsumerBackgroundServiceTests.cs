using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using CoreEvents.Shared.Contracts.Events;
using Events.Infrastructure.Data;
using Events.Infrastructure.Data.Entities;
using Events.IntegrationTests.Infrastructure.Bases;
using Events.IntegrationTests.Infrastructure.Factories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Events.IntegrationTests.BackgroundServices;

public class KafkaConsumerBackgroundServiceTests(IntegrationTestFactory factory) : SharedIntegrationTestBase(factory)
{
    private readonly IntegrationTestFactory _factory = factory;

    [Fact]
    public async Task Consumer_ShouldProcessBookingConfirmed_AndSaveToInbox()
    {
        // Arrange
        var producerConfig = new ProducerConfig { BootstrapServers = _factory.ConnectionStringKafka };
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var causationId = Guid.NewGuid();

        var payload = JsonSerializer.Serialize(new
        {
            EventId = eventId,
            BookingId = bookingId,
            UserId = userId,
            Seats = 2
        });

        var headers = new Headers
        {
            { "messageId", Encoding.UTF8.GetBytes(messageId.ToString()) },
            { "correlationId", Encoding.UTF8.GetBytes(correlationId.ToString()) },
            { "createdAt", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("O")) },
            { "causationId", Encoding.UTF8.GetBytes(causationId.ToString()) },
            { "eventType", Encoding.UTF8.GetBytes("BookingConfirmed") }
        };

        var message = new Message<string, string>
        {
            Key = eventId.ToString(),
            Value = payload,
            Headers = headers
        };

        // Act
        await producer.ProduceAsync(KafkaTopics.BookingConfirmed, message, TestContext.Current.CancellationToken);
        producer.Flush(TestContext.Current.CancellationToken);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

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
        inboxMessage.MessageType.Should().Be("BookingConfirmed");
        inboxMessage.Topic.Should().Be(KafkaTopics.BookingConfirmed);
        inboxMessage.MessageKey.Should().Be(eventId.ToString());

        var expectedDict = JsonSerializer.Deserialize<BookingConfirmed>(payload);
        var actualDict = JsonSerializer.Deserialize<BookingConfirmed>(inboxMessage.Payload);
        actualDict.Should().BeEquivalentTo(expectedDict);

        inboxMessage.ProcessedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }
}
