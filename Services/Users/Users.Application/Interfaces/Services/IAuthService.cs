using Users.Application.DTOs;

namespace Users.Application.Interfaces.Services;

public interface IAuthService
{
    Task<string> RegisterAsync(UserRegisterDto userRequestDto, CancellationToken ct = default);
    Task<string> LoginAsync(UserLoginDto userLoginDto, CancellationToken ct = default);
}
