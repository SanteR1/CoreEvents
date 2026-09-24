using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Users.Api.Controllers;

[ApiController]
[Authorize]
[ApiVersion(1.0)]
[Route("v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "User";

        return Ok(new
        {
            Id = userId,
            UserName = userName,
            Role = role
        });
    }
}

