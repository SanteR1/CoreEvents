using System.ComponentModel.DataAnnotations;

namespace CoreEvents.Application.DTOs;

public record UserRequestDto(
    [Required] string UserName,
    [Required] string Password,
    string? Role
);
