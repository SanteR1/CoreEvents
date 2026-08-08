using System.ComponentModel.DataAnnotations;

namespace CoreEvents.Application.DTOs;

public record UserLoginDto(
    [Required] string UserName,
    [Required] string Password
);
