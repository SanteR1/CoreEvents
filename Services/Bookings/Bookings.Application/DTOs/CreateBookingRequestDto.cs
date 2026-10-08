using System.ComponentModel.DataAnnotations;

namespace Bookings.Application.DTOs;

public record CreateBookingRequestDto(
    [Range(1, 100, ErrorMessage = "Количество мест должно быть в диапазоне от 1 до 100")]
    int? Seats = 1
);
