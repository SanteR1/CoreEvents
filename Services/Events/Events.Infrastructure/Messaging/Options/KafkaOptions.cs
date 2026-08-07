using System.ComponentModel.DataAnnotations;

namespace Events.Infrastructure.Messaging.Options
{
    internal sealed record KafkaOptions
    {
        [Required(AllowEmptyStrings = false)]
        public string BootstrapServers { get; init; } = string.Empty;

        [Required(AllowEmptyStrings = false)]
        public string GroupId { get; init; } = string.Empty;

        [Required]
        public TopicPair Topics { get; init; } = new();
    }
}
