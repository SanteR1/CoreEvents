using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Exceptions;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Identity.Enums;
using MediatR;

namespace Bookings.Application.Commands
{
    namespace Bookings.Application.Commands
    {
        public record CancelBookingByUserCommand(Guid BookingId, Guid UserId, RoleName UserRole) : ICommand<Guid>;

        internal class CancelBookingHandler(IBookingRepository repository, IOutboxService outboxService) : IRequestHandler<CancelBookingByUserCommand, Guid>
        {
            public async Task<Guid> Handle(CancelBookingByUserCommand request, CancellationToken ct)
            {
                var booking = await repository.GetByIdAsync(request.BookingId, ct);
                if (booking == null) throw new BookingNotFoundException(request.BookingId);

                var isAdmin = request.UserRole == RoleName.Admin;
                if (!isAdmin)
                {
                    booking.EnsureAccess(request.UserId);
                }

                var reason = booking.IsOwnedBy(request.UserId)
                    ? CancellationReason.UserCancelled
                    : CancellationReason.AdminCancelled;

                outboxService.Publish(
                    new BookingCancellationRequested()
                    {
                        BookingId = booking.Id,
                        EventId = booking.EventId,
                        UserId = booking.UserId,
                        Seats = booking.Seats,
                        Reason = reason,
                        CancelledAt = DateTimeOffset.UtcNow
                    },
                    partitionKey: booking.EventId.ToString());

                await repository.SaveChangesAsync(ct);

                return booking.Id;
            }
        }
    }
}
