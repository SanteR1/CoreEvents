using CoreEvents.Shared.Contracts.Exceptions;

namespace Users.Application.Exceptions
{
    public class UserAlreadyExistsException(string userName)
        : ConflictException($"User with username '{userName}' already exists.")
    {
        public override string ErrorCode => "Registration.UserAlreadyExists";
        public override object ErrorData => new { userName };
    }
}
