namespace CoreEvents.Domain.Exceptions;

public class DomainValidationException : DomainException
{
    public override string ErrorCode => "Domain.ValidationFailed";

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public DomainValidationException(string propertyName, string errorMessage)
        : base($"Validation failed for {propertyName}: {errorMessage}")
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, [errorMessage] }
        };
    }

    public DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more domain validation errors occurred.")
    {
        Errors = errors;
    }
}
