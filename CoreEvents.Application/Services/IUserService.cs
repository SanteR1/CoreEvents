using CoreEvents.Application.DTOs;

namespace CoreEvents.Application.Services;

public interface IUserService
{
    Task<string> RegisterAsync(UserRequestDto userRequestDto, CancellationToken ct = default);
    Task<string> LoginAsync(UserLoginDto userLoginDto, CancellationToken ct = default);
}
