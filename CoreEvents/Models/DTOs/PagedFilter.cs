namespace CoreEvents.Models.DTOs
{
    public record PagedFilter(
        int Page = 1,
        int PageSize = 10
        );
}
