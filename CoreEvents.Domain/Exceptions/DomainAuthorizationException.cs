namespace CoreEvents.Domain.Exceptions
{
    public class DomainAuthorizationException()
        : DomainException($"Wrong username or password.")
    {
        public override string ErrorCode => $"Authorization.Wrong";
    }
}
