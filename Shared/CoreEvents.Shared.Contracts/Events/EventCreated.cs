namespace CoreEvents.Shared.Contracts.Events;

public record EventCreated
{
    public required Guid EventId { get; init; }
    public required string Title { get; init; }
    public required DateTime StartAt { get; init; }
    public required DateTime EndAt { get; init; }
    public required decimal UnitPrice { get; init; }
    public required string Currency { get; init; }
    public required long PriceVersion { get; init; }
    public required int TotalSeats { get; init; }
    public required bool IsActive { get; init; }
    public required long Version { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
