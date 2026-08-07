using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Abstractions.Resilience.Attributes;
using Bookings.Application.Abstractions.Resilience.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bookings.Application.Commands
{
    namespace Bookings.Application.Commands
    {
        [ResiliencePipeline(ResiliencePipelines.CommandConcurrency)]
        public record ConfirmBookingCommand(Guid EventId, Guid BookingId) : ICommand<Unit>;

        internal class ConfirmBookingHandler(IBookingRepository repository, ILogger<ConfirmBookingHandler> logger)
            : IRequestHandler<ConfirmBookingCommand, Unit>
        {
            public async Task<Unit> Handle(ConfirmBookingCommand request, CancellationToken ct)
            {
                var booking = await repository.GetByIdAsync(request.BookingId, ct);
                if (booking == null)
                {
                    logger.LogWarning("Booking with ID {BookingId} not found for cancellation. Message ignored.",
                        request.BookingId);
                    return Unit.Value;
                }

                booking.Confirm();

                repository.Update(booking);

                await repository.SaveChangesAsync(ct);

                return Unit.Value;
            }
        }
    }
}
