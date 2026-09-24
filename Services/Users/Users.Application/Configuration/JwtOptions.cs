using System.ComponentModel.DataAnnotations;

namespace Users.Application.Configuration;

public class JwtOptions
{
    // Имя секции в appsettings / переменных окружения
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = "Секретный ключ JWT обязателен.")]
    [MinLength(32, ErrorMessage = "Секретный ключ должен содержать минимум 32 символа.")]
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// Идентификатор сервера, выпустившего токен
    /// </summary>
    [Required(ErrorMessage = "Issuer обязателен.")]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Идентификатор получателя токена
    /// </summary>
    [Required(ErrorMessage = "Audience обязателен.")]
    public string Audience { get; init; } = string.Empty;

    [Range(1, 10000, ErrorMessage = "Время жизни токена должно быть больше 0.")]
    public int ExpirationInMinutes { get; init; } = 15;

    [Range(1, 365, ErrorMessage = "Время жизни refresh токена должно быть от 1 до 365 дней.")]
    public int RefreshTokenExpirationInDays { get; init; } = 30;
}
