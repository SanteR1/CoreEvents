using Bookings.Application.Abstractions;
using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Configuration;
using Bookings.Application.DTOs;
using Bookings.Application.Exceptions;
using Bookings.Domain.Entities;
using Bookings.Domain.Exceptions;
using CoreEvents.Shared.Contracts.Events;
using MediatR;

namespace Bookings.Application.Commands;

public record CreateBookingCommand(Guid EventId, Guid UserId, int? Seats = 1) : ICommand<BookingResponseDto>;

internal class CreateBookingHandler(
    IBookingRepository repository,
    IEventProjectionRepository eventProjectionRepository,
    IOutboxService outboxService,
    BookingSettings bookingSettings) : IRequestHandler<CreateBookingCommand, BookingResponseDto>
{
    public async Task<BookingResponseDto> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var requestedSeats = request.Seats ?? 1;
        if (requestedSeats <= 0)
        {
            throw new ValidationException(nameof(request.Seats), "Количество мест должно быть больше 0.");
        }

        var bookingCount = await repository.GetBookingCountForUserAsync(request.UserId, cancellationToken);
        if (bookingCount >= bookingSettings.MaxBookingsPerUser)
        {
            throw new ActiveBookingLimitExceededException(bookingSettings.MaxBookingsPerUser);
        }

        var eventProjection = await eventProjectionRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (eventProjection == null)
        {
            throw new EventNotFoundException(request.EventId);
        }

        if (!eventProjection.IsActive)
        {
            throw new EventNotActiveException(request.EventId);
        }

        if (eventProjection.StartAt <= DateTime.UtcNow)
        {
            throw new PastEventBookingException(request.EventId);
        }

        if (requestedSeats > eventProjection.TotalSeats)
        {
            throw new ValidationException(nameof(request.Seats), "Запрошено больше мест, чем вместимость мероприятия.");
        }

        var booking = Booking.Create(request.EventId, request.UserId, requestedSeats);
        repository.Add(booking);

        outboxService.Publish(
            new BookingReservationRequested
            {
                BookingId = booking.Id,
                EventId = booking.EventId,
                UserId = booking.UserId,
                Seats = booking.Seats
            },
            partitionKey: booking.EventId.ToString());

        await repository.SaveChangesAsync(cancellationToken);

        return BookingResponseDto.FromEntity(booking);
    }
}
