using System.ComponentModel.DataAnnotations;

namespace CoreEvents.Models.DTOs
{
    public record BookingCreateDto(
        [Required]
        Guid EventId
    );

}
