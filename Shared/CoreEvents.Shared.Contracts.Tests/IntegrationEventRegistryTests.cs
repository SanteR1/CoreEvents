namespace CoreEvents.Shared.Contracts.Tests;

using System.Reflection;
using System.Reflection.Emit;
using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Registry;
using Xunit;

public class IntegrationEventRegistryTests
{
    [Fact]
    public void Registry_Should_Resolve_All_15_Integration_Events_Bidirectionally()
    {
        var registry = new IntegrationEventRegistry();
        var types = ContractTypes.AllConcreteIntegrationEvents();

        types.Should().HaveCount(15);

        foreach (var type in types)
        {
            var wireName = registry.GetName(type);
            wireName.Should().NotBeNullOrWhiteSpace();

            var resolvedType = registry.Resolve(wireName);
            resolvedType.Should().Be(type);

            registry.TryResolve(wireName, out var tryResolvedType).Should().BeTrue();
            tryResolvedType.Should().Be(type);
        }
    }

    [Fact]
    public void GetName_Typed_Should_Return_Same_As_Type_Method()
    {
        var registry = new IntegrationEventRegistry();

        registry.GetName<BookingCancelled>().Should().Be("booking.cancelled.v1");
#pragma warning disable CA2263 // Intentionally testing the Type overload
        registry.GetName(typeof(BookingCancelled)).Should().Be("booking.cancelled.v1");
#pragma warning restore CA2263
    }

    [Fact]
    public void Resolve_NonExistent_Event_Should_Throw_KeyNotFoundException()
    {
        var registry = new IntegrationEventRegistry();

        var act = () => registry.Resolve("non.existent.event.v1");

        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*non.existent.event.v1*");
    }

    [Fact]
    public void TryResolve_NonExistent_Event_Should_Return_False()
    {
        var registry = new IntegrationEventRegistry();

        var found = registry.TryResolve("non.existent.event.v1", out var eventType);

        found.Should().BeFalse();
        eventType.Should().BeNull();
    }

    [Fact]
    public void Registry_Throws_When_Event_Lacks_EventNameAttribute()
    {
        var dynamicAssembly = CreateDynamicAssemblyWithEvent(
            typeName: "EventWithoutAttribute",
            wireName: null);

        var act = () => new IntegrationEventRegistry([dynamicAssembly]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*не имеет обязательного атрибута [EventName]*");
    }

    [Fact]
    public void Registry_Throws_When_WireName_Is_Empty()
    {
        var dynamicAssembly = CreateDynamicAssemblyWithEvent(
            typeName: "EventWithEmptyName",
            wireName: "   ");

        var act = () => new IntegrationEventRegistry([dynamicAssembly]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*содержит пустое wire-имя*");
    }

    [Theory]
    [InlineData("booking.v1")]                  // меньше 2 сегментов до версии
    [InlineData("Booking.Cancelled.v1")]        // заглавные буквы
    [InlineData("booking.cancelled")]           // нет версии vN
    [InlineData("booking.cancelled.v0")]        // нулевая версия
    [InlineData("booking_cancelled.v1")]        // подчеркивание вместо дефиса
    [InlineData("booking..cancelled.v1")]       // двойная точка
    public void Registry_Throws_When_WireName_Has_Invalid_Format(string invalidWireName)
    {
        var dynamicAssembly = CreateDynamicAssemblyWithEvent(
            typeName: "EventWithInvalidWireName_" + Guid.NewGuid().ToString("N"),
            wireName: invalidWireName);

        var act = () => new IntegrationEventRegistry([dynamicAssembly]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*недопустимый формат wire-имени*");
    }

    [Fact]
    public void Registry_Throws_When_Duplicate_WireNames_Exist()
    {
        var dynamicAssembly = CreateDynamicAssemblyWithTwoEvents(
            typeName1: "EventOne_" + Guid.NewGuid().ToString("N"),
            typeName2: "EventTwo_" + Guid.NewGuid().ToString("N"),
            commonWireName: "test.duplicate-event.v1");

        var act = () => new IntegrationEventRegistry([dynamicAssembly]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Обнаружено дублирование wire-имени*");
    }

    private static AssemblyBuilder CreateDynamicAssemblyWithEvent(string typeName, string? wireName)
    {
        var asmName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
        var moduleBuilder = asmBuilder.DefineDynamicModule("MainModule");

        var typeBuilder = moduleBuilder.DefineType(
            typeName,
            TypeAttributes.Public | TypeAttributes.Class);

        typeBuilder.AddInterfaceImplementation(typeof(IIntegrationEvent));

        if (wireName != null)
        {
            var ctor = typeof(EventNameAttribute).GetConstructor([typeof(string)])!;
            var attrBuilder = new CustomAttributeBuilder(ctor, [wireName]);
            typeBuilder.SetCustomAttribute(attrBuilder);
        }

        typeBuilder.CreateType();
        return asmBuilder;
    }

    private static AssemblyBuilder CreateDynamicAssemblyWithTwoEvents(string typeName1, string typeName2, string commonWireName)
    {
        var asmName = new AssemblyName("TestAssembly_" + Guid.NewGuid().ToString("N"));
        var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
        var moduleBuilder = asmBuilder.DefineDynamicModule("MainModule");

        var ctor = typeof(EventNameAttribute).GetConstructor([typeof(string)])!;
        var attrBuilder = new CustomAttributeBuilder(ctor, [commonWireName]);

        var typeBuilder1 = moduleBuilder.DefineType(typeName1, TypeAttributes.Public | TypeAttributes.Class);
        typeBuilder1.AddInterfaceImplementation(typeof(IIntegrationEvent));
        typeBuilder1.SetCustomAttribute(attrBuilder);
        typeBuilder1.CreateType();

        var typeBuilder2 = moduleBuilder.DefineType(typeName2, TypeAttributes.Public | TypeAttributes.Class);
        typeBuilder2.AddInterfaceImplementation(typeof(IIntegrationEvent));
        typeBuilder2.SetCustomAttribute(attrBuilder);
        typeBuilder2.CreateType();

        return asmBuilder;
    }
}
