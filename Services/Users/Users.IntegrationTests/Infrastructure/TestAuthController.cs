using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Users.IntegrationTests.Infrastructure;

[ApiController]
[ApiVersionNeutral]
[Route("api/test-auth")]
public class TestAuthController : ControllerBase
{
    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdminOnly()
    {
        return Ok("Admin Access");
    }

    [HttpGet("anonymous")]
    [AllowAnonymous]
    public IActionResult GetAnonymous()
    {
        return Ok("Anonymous Access");
    }

    [HttpGet("protected")]
    [Authorize]
    public IActionResult GetProtected()
    {
        return Ok("Protected Access");
    }

    [HttpGet("user-only")]
    [Authorize(Roles = "User")]
    public IActionResult GetUserOnly()
    {
        return Ok("User Access");
    }
}
