using CoreEvents.Shared.Contracts.Events;
using Events.Domain.Enums;

namespace Events.Application.Common.Mappers;

internal static class ReserveSeatsResultMapper
{
    public static ValidationFailureReason ToValidationFailureReason(this ReserveSeatsResult result) => result switch
    {
        ReserveSeatsResult.EventInactive => ValidationFailureReason.EventNotFound,
        ReserveSeatsResult.InsufficientSeats => ValidationFailureReason.SeatsNotAvailable,
        ReserveSeatsResult.Reserved or ReserveSeatsResult.None =>
            throw new ArgumentOutOfRangeException(nameof(result), result, "Успешный или неинициализированный исход бронирования не может быть преобразован в причину отказа."),
        _ => throw new ArgumentOutOfRangeException(nameof(result), result, "Неизвестный исход бронирования.")
    };
}
