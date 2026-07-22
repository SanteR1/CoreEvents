namespace CoreEvents.Domain.Exceptions
{
    public class DomainUnauthorizedAccessException()
        : DomainException($"Authorized access only.")
    {
        public override string ErrorCode => $"Authorization.Denied";
    }
}
