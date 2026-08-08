namespace Events.Application.Abstractions.Resilience.Constants;

public static class ResiliencePipelines
{
    public const string GlobalTransient = "global-transient-pipeline";
    public const string CommandConcurrency = "command-concurrency-pipeline";
}
