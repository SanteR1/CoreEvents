using CoreEvents.Shared.Contracts.Exceptions;

namespace Bookings.Domain.Exceptions
{
    public class ValidationException : BadRequestException
    {
        public override string ErrorCode => "Booking.ValidationFailed";
        public override IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

        public ValidationException(string propertyName, string errorMessage)
            : base($"Validation failed for {propertyName}: {errorMessage}")
        {
            ValidationErrors = new Dictionary<string, string[]>
            {
                { propertyName, [errorMessage] }
            };
        }

        public ValidationException(IReadOnlyDictionary<string, string[]> errors)
            : base("One or more domain validation errors occurred.")
        {
            ValidationErrors = errors;
        }
    }
}
