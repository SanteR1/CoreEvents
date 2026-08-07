using CoreEvents.Shared.Contracts.Identity.Enums;

namespace Events.Application.Services;

public interface IUserContext
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    RoleName? Role { get; }
}
