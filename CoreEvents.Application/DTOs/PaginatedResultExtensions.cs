namespace CoreEvents.Application.DTOs
{
    public static class PaginatedResultExtensions
    {
        public static PaginatedResult<TDestination> Map<TSource, TDestination>(
            this PaginatedResult<TSource> source,
            Func<TSource, TDestination> mapFunc)
        {
            return new PaginatedResult<TDestination>()
            {
                CurrentPage = source.CurrentPage,
                PageSize = source.PageSize,
                TotalCount = source.TotalCount,
                Items = source.Items?.Select(mapFunc).ToList() ?? new List<TDestination>()
            };
        }
    }
}
