namespace Events.Application.Abstractions.Resilience.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ResiliencePipelineAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}
