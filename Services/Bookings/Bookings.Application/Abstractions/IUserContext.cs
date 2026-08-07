using CoreEvents.Shared.Contracts.Identity.Enums;

namespace Bookings.Application.Abstractions;

public interface IUserContext
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    RoleName? Role { get; }
}
