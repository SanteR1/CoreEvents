using System.Linq.Expressions;
using Users.Domain.Entities;
using Users.Domain.Enums;

namespace Users.Application.DTOs;

public record UserResponseDto(
    Guid Id,
    string UserName,
    RoleName Role
)
{
    public static Expression<Func<User, UserResponseDto>> ToDto => entity => new UserResponseDto(
        entity.Id,
        entity.UserName,
        entity.Role
    );

    public static UserResponseDto FromEntity(User entity) => new(
        entity.Id,
        entity.UserName,
        entity.Role
    );
}
