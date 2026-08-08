using System.ComponentModel.DataAnnotations;

namespace Users.Application.DTOs;

public record UserRegisterDto(
    [Required] string UserName,
    [Required] string Password
);
