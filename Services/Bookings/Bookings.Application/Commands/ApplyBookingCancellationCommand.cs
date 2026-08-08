using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Abstractions.Resilience.Attributes;
using Bookings.Application.Abstractions.Resilience.Constants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bookings.Application.Commands;

[ResiliencePipeline(ResiliencePipelines.CommandConcurrency)]
public record ApplyBookingCancellationCommand(Guid BookingId) : ICommand<Unit>;

internal class ApplyBookingCancellationCommandHandler(IBookingRepository repository, ILogger<ApplyBookingCancellationCommandHandler> logger) : IRequestHandler<ApplyBookingCancellationCommand, Unit>
{
    public async Task<Unit> Handle(ApplyBookingCancellationCommand request, CancellationToken ct)
    {
        var booking = await repository.GetByIdAsync(request.BookingId, ct);
        if (booking == null)
        {
            logger.LogWarning("Booking with ID {BookingId} not found for cancellation. Message ignored.", request.BookingId);
            return Unit.Value;
        }

        booking.Cancel();

        repository.Update(booking);

        await repository.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
