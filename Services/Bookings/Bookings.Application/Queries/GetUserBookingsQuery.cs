using Bookings.Application.Abstractions.Messaging;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Abstractions.Resilience.Attributes;
using Bookings.Application.Abstractions.Resilience.Constants;
using Bookings.Application.DTOs;
using CoreEvents.Shared.Contracts.Identity.Enums;
using MediatR;

namespace Bookings.Application.Queries;

[ResiliencePipeline(ResiliencePipelines.GlobalTransient)]
public sealed record GetUserBookingsQuery(
    Guid UserId,
    RoleName UserRole,
    BookingFilter Filter
) : IQuery<PaginatedResult<BookingResponseDto>>;

internal sealed class GetUserBookingsHandler(IBookingRepository repository)
    : IRequestHandler<GetUserBookingsQuery, PaginatedResult<BookingResponseDto>>
{
    public async Task<PaginatedResult<BookingResponseDto>> Handle(GetUserBookingsQuery request, CancellationToken cancellationToken)
    {
        var isAdmin = request.UserRole == RoleName.Admin;
        var pagedBookings = await repository.GetUserBookingsAsync(request.UserId, isAdmin, request.Filter, cancellationToken);
        return pagedBookings.Map(BookingResponseDto.FromEntity);
    }
}
