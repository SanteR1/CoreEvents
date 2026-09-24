using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Users.Application.Configuration;
using Users.Application.DTOs;
using Users.Application.Exceptions;
using Users.Application.Interfaces.Identity;
using Users.Application.Interfaces.Repositories;
using Users.Application.Interfaces.Services;
using Users.Domain.Entities;
using Users.Domain.Enums;

namespace Users.Application.Services;

internal class UserService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenProvider _token;
    private readonly IPasswordHasher _hasher;
    private readonly JwtOptions _jwtOptions;

    public UserService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenProvider token,
        IPasswordHasher hasher,
        IOptions<JwtOptions> jwtOptions)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _token = token;
        _hasher = hasher;
        _jwtOptions = jwtOptions.Value;
    }
    public async Task RegisterAsync(UserRegisterDto userRequestDto, CancellationToken ct = default)
    {
        var existUser = await _userRepository.GetByUserNameAsync(userRequestDto.UserName, ct);
        if (existUser != null) throw new UserAlreadyExistsException(userRequestDto.UserName);

        var user = User.Create(
            userName: userRequestDto.UserName,
            passwordHash: _hasher.Hash(userRequestDto.Password),
            role: nameof(RoleName.User)
        );

        _userRepository.Add(user);
        await _userRepository.SaveChangesAsync(ct);
    }

    public async Task<AuthResultDto> LoginAsync(UserLoginDto userLoginDto, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByUserNameAsync(userLoginDto.UserName, ct);
        if (user == null) throw new InvalidCredentialsException();

        if (!_hasher.Verify(password: userLoginDto.Password, hash: user.PasswordHash)) throw new InvalidCredentialsException();

        // 1. Миграция: если в базе старый SHA-256,
        // прозрачно для пользователя хешируем пароль новым Argon2id и сохраняем в БД
        if (_hasher.NeedsRehash(user.PasswordHash))
        {
            var newHash = _hasher.Hash(userLoginDto.Password);
            user.UpdatePasswordHash(newHash);
            await _userRepository.SaveChangesAsync(ct);
        }

        // 1. Создаем Access Token (JWT)
        var accessToken = _token.GenerateToken(new TokenPayload(user.Id, user.Role));

        // 2. Создаем и сохраняем Refresh Token в БД
        var rawRefreshToken = GenerateRawToken();
        var refreshTokenHash = HashToken(rawRefreshToken);
        var refreshToken = RefreshToken.Create(user.Id, refreshTokenHash, TimeSpan.FromDays(_jwtOptions.RefreshTokenExpirationInDays));

        _refreshTokenRepository.Add(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync(ct);

        return new AuthResultDto(
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            User: UserResponseDto.FromEntity(user)
        );
    }

    public async Task<AuthResultDto> RefreshSessionAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        var tokenHash = HashToken(rawRefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);

        if (existingToken == null)
            throw new InvalidCredentialsException();

        // Детекция кражи токена (Reuse Detection):
        // Если токен уже был отозван, значит, кто-то повторно использует старый токен!
        if (existingToken.IsRevoked)
        {
            // Немедленно аннулируем ВСЕ активные сессии скомпрометированного пользователя
            await _refreshTokenRepository.RevokeAllActiveByUserIdAsync(existingToken.UserId, ct);
            await _refreshTokenRepository.SaveChangesAsync(ct);
            throw new InvalidCredentialsException();
        }

        if (existingToken.IsExpired)
            throw new InvalidCredentialsException();

        var user = await _userRepository.GetByIdAsync(existingToken.UserId, ct);
        if (user == null)
            throw new InvalidCredentialsException();

        // Ротация: выпускаем новый токен и связываем его со старым
        var newRawToken = GenerateRawToken();
        var newTokenHash = HashToken(newRawToken);

        existingToken.Rotate(newTokenHash);

        var newToken = RefreshToken.Create(user.Id, newTokenHash, TimeSpan.FromDays(_jwtOptions.RefreshTokenExpirationInDays));
        _refreshTokenRepository.Add(newToken);

        await _refreshTokenRepository.SaveChangesAsync(ct);

        var newAccessToken = _token.GenerateToken(new TokenPayload(user.Id, user.Role));

        return new AuthResultDto(
            AccessToken: newAccessToken,
            RefreshToken: newRawToken,
            User: UserResponseDto.FromEntity(user)
        );
    }

    public async Task RevokeSessionAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        var tokenHash = HashToken(rawRefreshToken);
        var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);

        if (token != null && !token.IsRevoked)
        {
            token.Revoke();
            await _refreshTokenRepository.SaveChangesAsync(ct);
        }
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
                      .Replace("+", "-")
                      .Replace("/", "_")
                      .TrimEnd('='); // Безопасный URL Base64
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
