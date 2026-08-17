using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Configuration;
using Bookings.Application.DTOs;
using Bookings.Application.Exceptions;
using Bookings.Domain.Entities;
using CoreEvents.Shared.Contracts.Events;
using MediatR;

namespace Bookings.Application.Commands;

public record CreateBookingCommand(Guid EventId, Guid UserId, int? Seats = 1) : ICommand<BookingResponseDto>;

internal class CreateBookingHandler(IBookingRepository repository, IOutboxService outboxService, BookingSettings bookingSettings) : IRequestHandler<CreateBookingCommand, BookingResponseDto>
{
    public async Task<BookingResponseDto> Handle(CreateBookingCommand request, CancellationToken ct)
    {
        var bookingCount = await repository.GetBookingCountForUserAsync(request.UserId, ct);
        if (bookingCount >= bookingSettings.MaxBookingsPerUser) throw new ActiveBookingLimitExceededException(bookingSettings.MaxBookingsPerUser);

        var booking = Booking.Create(request.EventId, request.UserId, request.Seats ?? 1);

        repository.Add(booking);

        outboxService.Publish(
            new BookingConfirmed()// BookingCreated()
            {
                BookingId = booking.Id,
                EventId = booking.EventId,
                UserId = booking.UserId,
                Seats = request.Seats ?? 1
            },
            partitionKey: request.EventId.ToString());

        await repository.SaveChangesAsync(ct);

        return BookingResponseDto.FromEntity(booking);
    }
}
