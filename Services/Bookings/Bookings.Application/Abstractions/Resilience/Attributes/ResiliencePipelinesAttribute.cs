namespace Bookings.Application.Abstractions.Resilience.Attributes;

// для удобства, если нужно передавать сразу список
[AttributeUsage(AttributeTargets.Class)]
public sealed class ResiliencePipelinesAttribute(params string[] keys) : Attribute
{
    public string[] Keys { get; } = keys;
}
