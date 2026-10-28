using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Infrastructure;
using Events.Infrastructure.Messaging.Kafka;
using Xunit;

namespace Events.Tests.Messaging;

public class EventTopicMapperTests
{
    private readonly EventTopicMapper _mapper = new();

    [Theory]
    [InlineData(typeof(EventBookingValidationCompleted), KafkaTopics.EventConfirmed)]
    [InlineData(typeof(EventBookingCancellationCompleted), KafkaTopics.EventConfirmed)]
    [InlineData(typeof(BookingReservationAccepted), KafkaTopics.EventTopic)]
    [InlineData(typeof(BookingReservationRejected), KafkaTopics.EventTopic)]
    [InlineData(typeof(BookingReservationReleased), KafkaTopics.EventTopic)]
    [InlineData(typeof(EventCreated), KafkaTopics.EventTopic)]
    [InlineData(typeof(EventUpdated), KafkaTopics.EventTopic)]
    [InlineData(typeof(EventCancelled), KafkaTopics.EventTopic)]
    public void GetTopicFor_ConfiguredEvents_ShouldReturnCorrectTopic(Type eventType, string expectedTopic)
    {
        // Act
        var method = typeof(EventTopicMapper).GetMethod(nameof(EventTopicMapper.GetTopicFor))!
            .MakeGenericMethod(eventType);
        var topic = (string)method.Invoke(_mapper, null)!;

        // Assert
        topic.Should().Be(expectedTopic);
        topic.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetTopicFor_UnconfiguredEvent_ShouldThrowInvalidOperationException()
    {
        // Act
        var act = () => _mapper.GetTopicFor<BookingCancelled>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Topic for event BookingCancelled is not configured*");
    }

    [Fact]
    public void AllConfiguredTopics_MustBeValidKafkaTopicNames()
    {
        // All published events
        var publishedTopics = new[]
        {
            _mapper.GetTopicFor<EventBookingValidationCompleted>(),
            _mapper.GetTopicFor<EventBookingCancellationCompleted>(),
            _mapper.GetTopicFor<BookingReservationAccepted>(),
            _mapper.GetTopicFor<BookingReservationRejected>(),
            _mapper.GetTopicFor<BookingReservationReleased>(),
            _mapper.GetTopicFor<EventCreated>(),
            _mapper.GetTopicFor<EventUpdated>(),
            _mapper.GetTopicFor<EventCancelled>()
        };

        foreach (var topic in publishedTopics)
        {
            topic.Should().NotBeNullOrWhiteSpace();
            topic.Should().MatchRegex(@"^[a-zA-Z0-9._-]+$", "Имя Kafka топика должно содержать только допустимые символы");
        }
    }
}
