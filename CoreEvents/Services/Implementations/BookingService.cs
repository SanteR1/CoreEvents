using CoreEvents.Data.DataAccess;
using CoreEvents.Middleware;
using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;
using CoreEvents.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoreEvents.Services.Implementations
{
    internal sealed class BookingService : IBookingService
    {
        private readonly AppDbContext _context;
        private static readonly SemaphoreSlim Semaphore = new(1, 1);
        public BookingService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto bookingDto, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Semaphore.WaitAsync(cancellationToken);
            try
            {
                var existEvent = await _context.Events.FirstOrDefaultAsync(x => x.Id == bookingDto.EventId, cancellationToken);
                if (existEvent is null) throw new NotFoundException($"Событие с ID {bookingDto.EventId} не найдено.");

                if (!existEvent.TryReserveSeats())
                {
                    throw new NoAvailableSeatsException("No available seats for this event.");
                }

                var booking = Booking.Create(bookingDto.EventId);
                await _context.Bookings.AddAsync(booking, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                return BookingResponseDto.FromEntity(booking);
            }
            finally
            {
                Semaphore.Release();
            }
        }

        public async Task<BookingResponseDto> GetBookingByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var booking = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (booking == null)
                throw new NotFoundException($"Бронь с ID {id} не найдена.");
            return BookingResponseDto.FromEntity(booking);
        }
    }
}