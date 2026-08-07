namespace Bookings.Application.Abstractions.Resilience.Constants
{
    public static class ResiliencePipelines
    {
        public const string GlobalTransient = "global-transient-pipeline";
        public const string CommandConcurrency = "command-concurrency-pipeline";
        public const string DltRetry = "dlt-retry-pipeline";
    }
}
