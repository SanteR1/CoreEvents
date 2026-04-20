using System.ComponentModel.DataAnnotations;

namespace CoreEvents.Models.DTOs
{
    public class PagedFilter
    {
        [Range(1, 100000, ErrorMessage = "Значение должно быть в диапазоне от 1 до 100000")]
        public int Page { get; init; } = 1;

        [Range(1, 100, ErrorMessage = "Значение должно быть в диапазоне от 1 до 100")]
        public int PageSize { get; init; } = 10;
    }
}
