using Polly;
using Polly.DependencyInjection;

namespace Events.Application.Abstractions.Resilience
{
    public interface IResiliencePipelineConfiguration
    {
        string PipelineKey { get; }
        void Configure(ResiliencePipelineBuilder builder, AddResiliencePipelineContext<string> context);
    }
}
