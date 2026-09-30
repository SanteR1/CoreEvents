using System.ComponentModel.DataAnnotations;

namespace Bookings.Infrastructure.Messaging.Options;

internal sealed record KafkaOptions
{
    [Required(AllowEmptyStrings = false)]
    public string BootstrapServers { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string GroupId { get; init; } = string.Empty;

    public bool InitKafkaTopics { get; init; } = true;

    [Range(1, 60)]
    public int TopicCreationTimeoutSeconds { get; init; } = 10;

    [Required]
    public TopicPair Topics { get; init; } = new();
}
