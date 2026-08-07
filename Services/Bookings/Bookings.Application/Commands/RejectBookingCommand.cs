using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Abstractions.Resilience.Attributes;
using Bookings.Application.Abstractions.Resilience.Constants;
using CoreEvents.Shared.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bookings.Application.Commands
{
    namespace Bookings.Application.Commands
    {
        [ResiliencePipeline(ResiliencePipelines.CommandConcurrency)]
        public record RejectBookingCommand(Guid BookingId, ValidationFailureReason Reason) : ICommand<Unit>;

        internal class RejectBookingHandler(IBookingRepository repository, ILogger<ApplyBookingCancellationCommandHandler> logger) : IRequestHandler<RejectBookingCommand, Unit>
        {
            public async Task<Unit> Handle(RejectBookingCommand request, CancellationToken ct)
            {
                var booking = await repository.GetByIdAsync(request.BookingId, ct);
                if (booking == null)
                {
                    logger.LogWarning("Booking with ID {BookingId} not found for cancellation. Message ignored.", request.BookingId);
                    return Unit.Value;
                }

                booking.Reject();

                repository.Update(booking);

                await repository.SaveChangesAsync(ct);

                return Unit.Value;
            }
        }
    }
}
