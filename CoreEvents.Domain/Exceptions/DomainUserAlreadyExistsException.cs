namespace CoreEvents.Domain.Exceptions
{ public class DomainUserAlreadyExistsException(string userName)
        : DomainException($"User with username '{userName}' already exists.")
    {
        public override string ErrorCode => "Registration.UserAlreadyExists";
    }
}
