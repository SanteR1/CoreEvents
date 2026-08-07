using System.ComponentModel.DataAnnotations;

namespace Bookings.Infrastructure.Messaging.Options;

internal sealed record TopicConfig
{
    [Range(1, 1000)]
    public int Partitions { get; init; } = 1;

    [Range(1, 100)]
    public short ReplicationFactor { get; init; } = 1;
}
