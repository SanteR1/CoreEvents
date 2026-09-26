using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Users.Application.Interfaces.Repositories;

namespace Users.Api.Controllers;

[ApiController]
[Authorize]
[ApiVersion(1.0)]
[Route("v{version:apiVersion}/users")]
public class UsersController(IUserRepository userRepository) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = User.FindFirstValue("name")
                       ?? User.FindFirstValue(ClaimTypes.Name)
                       ?? User.FindFirstValue(JwtRegisteredClaimNames.Email)
                       ?? User.FindFirstValue("email")
                       ?? User.Identity?.Name;
        var role = User.FindFirstValue("role")
                   ?? User.FindFirstValue(ClaimTypes.Role)
                   ?? "User";

        if (string.IsNullOrWhiteSpace(userName) && Guid.TryParse(userId, out var parsedId))
        {
            var user = await userRepository.GetByIdAsync(parsedId, ct);
            if (user != null)
            {
                userName = user.UserName;
            }
        }

        return Ok(new
        {
            Id = userId,
            UserName = userName,
            Role = role
        });
    }
}

