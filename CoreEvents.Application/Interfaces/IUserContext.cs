using CoreEvents.Domain.Enums;

namespace CoreEvents.Application.Interfaces
{
    public interface IUserContext
    {
        Guid? UserId { get; }
        bool IsAuthenticated { get; }
        RoleName? Role { get; }
    }
}
