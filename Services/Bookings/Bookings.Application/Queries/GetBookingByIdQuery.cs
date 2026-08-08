using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Abstractions.Resilience.Attributes;
using Bookings.Application.Abstractions.Resilience.Constants;
using Bookings.Application.DTOs;
using Bookings.Application.Exceptions;
using CoreEvents.Shared.Contracts.Identity.Enums;
using MediatR;

namespace Bookings.Application.Queries
{
    namespace Bookings.Application.Queries
    {
        [ResiliencePipeline(ResiliencePipelines.GlobalTransient)]
        public sealed record GetBookingByIdQuery(Guid BookingId, Guid UserId, RoleName UserRole) : IQuery<BookingResponseDto>;
        internal sealed class GetBookingByIdHandler(IBookingRepository repository)
            : IRequestHandler<GetBookingByIdQuery, BookingResponseDto>
        {
            public async Task<BookingResponseDto> Handle(GetBookingByIdQuery request, CancellationToken ct)
            {
                var booking = await repository.GetByIdAsync(request.BookingId, ct)
                              ?? throw new BookingNotFoundException(request.BookingId);

                var isAdmin = request.UserRole == RoleName.Admin;
                if (!isAdmin)
                {
                    booking.EnsureAccess(request.UserId);
                }
                return BookingResponseDto.FromEntity(booking);
            }
        }
    }
}
