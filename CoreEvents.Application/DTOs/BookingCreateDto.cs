using System.ComponentModel.DataAnnotations;

namespace CoreEvents.Application.DTOs
{
    public record BookingCreateDto(
        [Required]
        Guid EventId
    );

}
