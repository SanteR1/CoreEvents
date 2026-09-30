namespace Users.Application.Interfaces.Identity;

public interface ITokenProvider
{
    string GenerateToken(TokenPayload payload);
}

