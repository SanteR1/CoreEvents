namespace Events.Domain.Enums;

public enum ReserveSeatsResult
{
    /// <summary>
    /// Значение по умолчанию. Не является успехом: <c>default(ReserveSeatsResult)</c>
    /// не должен трактоваться как успешная резервация (см. CA1008).
    /// </summary>
    None = 0,
    Reserved = 1,
    EventInactive = 2,
    InsufficientSeats = 3
}
