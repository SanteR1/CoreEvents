namespace Events.Infrastructure.Caching.Options;

internal sealed record EventCacheOptions
{
    public const string SectionName = "Cache:Event";
    public TimeSpan EventTtl { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan EventTopTtl { get; set; } = TimeSpan.FromMinutes(60);
}
