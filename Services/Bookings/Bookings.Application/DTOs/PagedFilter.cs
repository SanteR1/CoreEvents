namespace Bookings.Application.DTOs;

public record PagedFilter
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
