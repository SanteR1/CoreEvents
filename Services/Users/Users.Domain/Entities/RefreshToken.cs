using Users.Domain.Exceptions;

namespace Users.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    // Вычисляемые доменные свойства
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    // Приватный конструктор для материализации EF Core
    private RefreshToken() { }

    /// <summary>
    /// Фабричный метод с защитой инвариантов
    /// </summary>
    public static RefreshToken Create(Guid userId, string tokenHash, TimeSpan lifetime)
    {
        if (userId == Guid.Empty)
            throw new ValidationException(nameof(userId), "Идентификатор пользователя не может быть пустым.");

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ValidationException(nameof(tokenHash), "Хеш токена не может быть пустым.");

        if (lifetime <= TimeSpan.Zero)
            throw new ValidationException(nameof(lifetime), "Срок действия токена должен быть положительным.");

        var utcNow = DateTime.UtcNow;

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = utcNow,
            ExpiresAt = utcNow.Add(lifetime)
        };
    }

    /// <summary>
    /// Доменная операция ротации: отзывает токен и связывает его со следующим токеном в цепочке RTR
    /// </summary>
    public void Rotate(string newTokenHash)
    {
        if (string.IsNullOrWhiteSpace(newTokenHash))
            throw new ValidationException(nameof(newTokenHash), "Хеш нового токена не может быть пустым.");

        if (!IsActive)
            throw new InvalidOperationException("Невозможно ротировать неактивный или уже отозванный токен.");

        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenHash = newTokenHash;
    }

    /// <summary>
    /// Доменная операция явного отзыва (при Logout или обнаружении компрометации)
    /// </summary>
    public void Revoke()
    {
        if (IsRevoked) return;
        RevokedAt = DateTime.UtcNow;
    }
}
