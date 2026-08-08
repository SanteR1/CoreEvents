using Users.Domain.Enums;
using Users.Domain.Exceptions;

namespace Users.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public RoleName Role { get; private set; }
    private User() { }

    private User(Guid id, string userName, string passwordHash, RoleName role)
    {
        Id = id;
        UserName = userName;
        PasswordHash = passwordHash;
        Role = role;
    }

    public static User Create(string userName, string passwordHash, string? role = "User")
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new DomainValidationException(nameof(userName), "Логин должен быть указан.");

        var userRole = role switch
        {
            "User" => RoleName.User,
            "Admin" => RoleName.Admin,
            _ => RoleName.User
        };

        return new User()
        {
            Id = Guid.NewGuid(),
            PasswordHash = passwordHash,
            UserName = userName,
            Role = userRole
        };
    }
}
