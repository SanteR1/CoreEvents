using System.Security.Claims;
using Bookings.Application.Abstractions;
using CoreEvents.Shared.Contracts.Identity.Enums;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Bookings.Api.Services;

public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid? UserId
    {
        get
        {
            var idClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (Guid.TryParse(idClaim, out var userId))
                return userId;

            return null;
        }
    }

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public RoleName? Role
    {
        get
        {
            var roleClaim = httpContextAccessor.HttpContext?.User.FindFirstValue("role");

            if (string.IsNullOrEmpty(roleClaim))
                return null;

            if (Enum.TryParse<RoleName>(roleClaim, ignoreCase: true, out var role))
                return role;

            throw new ArgumentOutOfRangeException(nameof(Role), $"Неизвестная роль в токене: {roleClaim}");
        }
    }
}
