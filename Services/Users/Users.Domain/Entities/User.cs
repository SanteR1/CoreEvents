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
            throw new ValidationException(nameof(userName), "Логин должен быть указан.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ValidationException(nameof(passwordHash), "Пароль должен быть указан.");

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

    public void UpdatePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ValidationException(nameof(newPasswordHash), "Хеш пароля не может быть пустым.");

        PasswordHash = newPasswordHash;
    }
}
