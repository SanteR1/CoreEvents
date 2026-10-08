using System.ComponentModel.DataAnnotations;

namespace Events.Application.DTOs;

public record EventUpdateDto(
    [Required(AllowEmptyStrings = false, ErrorMessage = "Название обязательно")]
    string? Title,
    [Required(ErrorMessage = "Дата начала обязательна")]
    DateTime? StartAt,
    [Required(ErrorMessage = "Дата окончания обязательна")]
    DateTime? EndAt,
    string? Description = null,
    [Range(0, 100000000, ErrorMessage = "Цена не может быть отрицательной")]
    decimal? Price = null,
    [MaxLength(3, ErrorMessage = "Код валюты должен состоять максимум из 3 символов")]
    string? Currency = null);
