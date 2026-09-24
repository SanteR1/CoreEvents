namespace Users.Application.DTOs;

public record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    UserResponseDto User
);
