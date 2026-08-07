using Polly;
using Polly.DependencyInjection;

namespace Bookings.Infrastructure.Resilience.Abstractions
{
    public interface IResiliencePipelineConfiguration
    {
        string PipelineKey { get; }
        void Configure(ResiliencePipelineBuilder builder, AddResiliencePipelineContext<string> context);
    }
}
