using System.ComponentModel.DataAnnotations;

namespace Users.Application.DTOs
{
    public record UserLoginDto(
        [Required] string UserName,
        [Required] string Password
    );
}
