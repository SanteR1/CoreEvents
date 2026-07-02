
namespace CoreEvents.Domain.Exceptions
{
    public class DomainNotFoundException(string entityName, string paramName, object? key)
        : DomainException($"Entity '{entityName}' with {paramName} = '{key}' was not found.")
    {
        public override string ErrorCode => $"{EntityName}.NotFound";

        public string EntityName { get; } = entityName;
        public string ParamName { get; } = paramName;
        public string Key { get; } = key?.ToString() ?? string.Empty;
    }
}
