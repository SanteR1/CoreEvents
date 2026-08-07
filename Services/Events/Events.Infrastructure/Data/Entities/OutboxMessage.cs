namespace Events.Infrastructure.Data.Entities
{
    public sealed class OutboxMessage
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public Guid CorrelationId { get; init; }
        public Guid? CausationId { get; init; }

        public required string MessageType { get; init; }
        public required string Topic { get; init; }
        public required string Key { get; init; }
        public required string Payload { get; init; }
        public required string Headers { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? PublishedAt { get; set; }

        public int RetryCount { get; set; }
        public DateTimeOffset? NextRetryAt { get; set; }
        public string? LastError { get; set; }
        public bool IsDeadLettered { get; set; }
    }
}
