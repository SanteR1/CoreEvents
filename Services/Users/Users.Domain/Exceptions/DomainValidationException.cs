using CoreEvents.Shared.Contracts.Exceptions;

namespace Users.Domain.Exceptions
{
    public class DomainValidationException : BadRequestException
    {
        public override string ErrorCode => "Domain.ValidationFailed";

        // Переопределяем свойство ValidationErrors из AppException,
        // чтобы обработчик сам нашел этот словарь и положил его в ProblemDetails.
        public override IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

        public DomainValidationException(string propertyName, string errorMessage)
            : base($"Validation failed for {propertyName}: {errorMessage}")
        {
            ValidationErrors = new Dictionary<string, string[]>
            {
                { propertyName, [errorMessage] }
            };
        }

        public DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
            : base("One or more domain validation errors occurred.")
        {
            ValidationErrors = errors;
        }
    }
}
