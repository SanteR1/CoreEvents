using System.ComponentModel.DataAnnotations;

namespace Bookings.Application.DTOs;

public record BookingCreateDto(
    [Required]
    Guid EventId
);
