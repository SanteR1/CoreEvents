using CoreEvents.Shared.Contracts.Exceptions;

namespace Users.Application.Exceptions;

public class InvalidCredentialsException()
    : UnauthorizedException($"Wrong username or password.")
{
    public override string ErrorCode => $"Authorization.Wrong";
}
