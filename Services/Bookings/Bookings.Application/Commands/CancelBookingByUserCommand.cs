using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.DTOs;
using Bookings.Application.Exceptions;
using Bookings.Domain.Enums;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Identity.Enums;
using MediatR;

namespace Bookings.Application.Commands;

public record CancelBookingByUserCommand(
    Guid BookingId,
    Guid UserId,
    RoleName UserRole,
    CancellationReason? Reason = null
) : ICommand<BookingResponseDto>;

internal class CancelBookingHandler(IBookingRepository repository, IOutboxService outboxService) : IRequestHandler<CancelBookingByUserCommand, BookingResponseDto>
{
    public async Task<BookingResponseDto> Handle(CancelBookingByUserCommand request, CancellationToken cancellationToken)
    {
        var booking = await repository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking == null)
        {
            throw new BookingNotFoundException(request.BookingId);
        }

        var isAdmin = request.UserRole == RoleName.Admin;
        if (!isAdmin)
        {
            booking.EnsureAccess(request.UserId);
        }

        if (booking.Status == BookingStatus.Pending)
        {
            throw new BookingCancellationConflictException("Бронирование находится в процессе обработки. Дождитесь подтверждения перед отменой.");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new BookingCancellationConflictException($"Невозможно отменить бронирование со статусом '{booking.Status}'.");
        }

        var defaultReason = booking.IsOwnedBy(request.UserId)
            ? CancellationReason.UserCancelled
            : CancellationReason.AdminCancelled;
        var reason = request.Reason ?? defaultReason;

        booking.RequestCancellation(reason);

        outboxService.Publish(
            new BookingReservationReleaseRequested
            {
                BookingId = booking.Id,
                EventId = booking.EventId,
                Seats = booking.Seats,
                Reason = reason
            },
            partitionKey: booking.EventId.ToString());

        repository.Update(booking);
        await repository.SaveChangesAsync(cancellationToken);

        return BookingResponseDto.FromEntity(booking);
    }
}
