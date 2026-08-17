using Users.Domain.Enums;

namespace Users.Application.Interfaces;

public interface IUserContext
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    RoleName? Role { get; }
}
