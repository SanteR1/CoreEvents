namespace Events.Application.Abstractions.Caching;

public static class CacheKeys
{
    public static string Event(Guid id) => $"event:{id}";

    public const string Top10Events = "events:top10";
}
