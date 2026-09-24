using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Users.Api.Services;
using Users.Application.DTOs;
using Users.Application.Interfaces.Services;

namespace Users.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("v{version:apiVersion}/[controller]")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class AuthController : ControllerBase
{
    private readonly IAuthService _userService;
    private readonly IAuthCookieService _cookieService;

    public AuthController(IAuthService userService, IAuthCookieService cookieService)
    {
        _userService = userService;
        _cookieService = cookieService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto user, CancellationToken ct)
    {
        await _userService.RegisterAsync(user, ct);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> Login([FromBody] UserLoginDto user, CancellationToken ct)
    {
        var result = await _userService.LoginAsync(user, ct);
        _cookieService.SetAuthCookies(result.AccessToken, result.RefreshToken);
        return Ok(result.User);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var refreshToken = _cookieService.GetRefreshToken();
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized(new ProblemDetails { Detail = "Refresh token не найден в Cookies." });

        var result = await _userService.RefreshSessionAsync(refreshToken, ct);
        _cookieService.SetAuthCookies(result.AccessToken, result.RefreshToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var refreshToken = _cookieService.GetRefreshToken();
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await _userService.RevokeSessionAsync(refreshToken, ct);
        }

        _cookieService.ClearAuthCookies();
        return NoContent();
    }
}
