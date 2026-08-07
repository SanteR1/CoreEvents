using System.ComponentModel.DataAnnotations;

namespace Events.Infrastructure.Messaging.Options
{
    internal sealed record TopicPair
    {
        [Required]
        public TopicConfig MainTopic { get; init; } = new();

        [Required]
        public TopicConfig DeadLetterTopic { get; init; } = new();
    }
}
