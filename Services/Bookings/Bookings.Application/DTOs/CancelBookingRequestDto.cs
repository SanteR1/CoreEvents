using CoreEvents.Shared.Contracts.Events;

namespace Bookings.Application.DTOs;

public record CancelBookingRequestDto(
    CancellationReason? Reason = CancellationReason.UserCancelled
);
