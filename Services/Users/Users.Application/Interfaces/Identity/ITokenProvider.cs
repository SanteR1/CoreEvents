using Users.Domain.Enums;

namespace Users.Application.Interfaces.Identity;

public record TokenPayload
{
    public Guid UserId { get; }
    public RoleName Role { get; }
    public string? Email { get; }

    public TokenPayload(Guid userId, RoleName role = RoleName.User, string? email = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId не может быть пустым", nameof(userId));

        UserId = userId;
        Role = role;
        Email = email;
    }
}
public interface ITokenProvider
{
    string GenerateToken(TokenPayload payload);
}
