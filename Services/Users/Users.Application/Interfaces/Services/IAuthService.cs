using Users.Application.DTOs;

namespace Users.Application.Interfaces.Services;

public interface IAuthService
{
    Task RegisterAsync(UserRegisterDto userRequestDto, CancellationToken ct = default);
    Task<AuthResultDto> LoginAsync(UserLoginDto userLoginDto, CancellationToken ct = default);
    Task<AuthResultDto> RefreshSessionAsync(string rawRefreshToken, CancellationToken ct = default);
    Task RevokeSessionAsync(string rawRefreshToken, CancellationToken ct = default);
}
