namespace CoreEvents.Shared.Contracts.Tests;

using System.Reflection;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using Xunit;

public class EventContractMarkerTests
{
    private static readonly Regex WireNameVersionRegex = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*\.[a-z0-9]+(?:-[a-z0-9]+)*(?:\.[a-z0-9]+(?:-[a-z0-9]+)*)*\.v[1-9][0-9]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    [Fact]
    public void All_Integration_Events_Should_Be_Exactly_15()
    {
        var types = ContractTypes.AllConcreteIntegrationEvents();
        types.Should().HaveCount(15);
    }

    [Fact]
    public void Every_Concrete_Type_In_Events_Namespace_Must_Implement_IIntegrationEvent()
    {
        var types = ContractTypes.AllConcreteIntegrationEvents();

        foreach (var type in types)
        {
            typeof(IIntegrationEvent).IsAssignableFrom(type)
                .Should().BeTrue($"Тип {type.Name} должен реализовывать {nameof(IIntegrationEvent)}");
        }
    }

    [Fact]
    public void Every_Concrete_Integration_Event_Must_Have_Valid_EventNameAttribute()
    {
        var types = ContractTypes.AllConcreteIntegrationEvents();

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<EventNameAttribute>(inherit: false);
            attr.Should().NotBeNull($"Событие {type.Name} должно быть помечено атрибутом [EventName]");
            if (attr != null)
            {
                attr.Name.Should().NotBeNullOrWhiteSpace();
                WireNameVersionRegex.IsMatch(attr.Name)
                    .Should().BeTrue($"Wire-имя события {type.Name} ('{attr.Name}') должно соответствовать формату '<domain>.<event-name>.v<version>'");
            }
        }
    }
}
