namespace CoreEvents.Shared.Contracts.Tests;

using System.Globalization;
using System.Reflection;
using System.Text.Json;
using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Serialization;
using Xunit;

public class EventSerializationRoundTripTests
{
    public static TheoryData<Type> AllEventTypes()
    {
        var data = new TheoryData<Type>();
        foreach (var type in ContractTypes.AllConcreteIntegrationEvents())
        {
            data.Add(type);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(AllEventTypes))]
    public void Event_Should_RoundTrip_Serialize_And_Deserialize_With_NonDefault_Values(Type eventType)
    {
        // 1. Создаем экземпляр с не-дефолтными значениями всех свойств
        var instance = CreateInstanceWithNonDefaultValues(eventType);

        // 2. Проверяем, что генератор заполнил ВСЕ публичные свойства не-дефолтными значениями
        AssertNotDefault(instance);

        // 3. Сериализуем в JSON с IntegrationEventJsonOptions.Default
        var json = JsonSerializer.Serialize(instance, eventType, IntegrationEventJsonOptions.Default);
        json.Should().NotBeNullOrWhiteSpace();

        // 4. Десериализуем обратно
        var deserialized = JsonSerializer.Deserialize(json, eventType, IntegrationEventJsonOptions.Default);
        deserialized.Should().NotBeNull();

        // 5. Проверяем идентичность всех свойств
        deserialized.Should().BeEquivalentTo(instance);
    }

    private static object CreateInstanceWithNonDefaultValues(Type type)
    {
        var instance = Activator.CreateInstance(type)!;

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.CanWrite)
            {
                var val = GenerateNonDefaultValue(prop.PropertyType);
                prop.SetValue(instance, val);
            }
        }

        return instance;
    }

    private static object GenerateNonDefaultValue(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(Guid))
            return Guid.NewGuid();
        if (underlying == typeof(string))
            return "test_val_" + Guid.NewGuid().ToString("N")[..8];
        if (underlying == typeof(int))
            return 42;
        if (underlying == typeof(long))
            return 100L;
        if (underlying == typeof(decimal))
            return 250.50m;
        if (underlying == typeof(bool))
            return true;
        if (underlying == typeof(DateTime))
            return new DateTime(2026, 10, 15, 12, 30, 0, DateTimeKind.Utc);
        if (underlying == typeof(DateTimeOffset))
            return new DateTimeOffset(2026, 10, 15, 12, 30, 0, TimeSpan.Zero);
        if (underlying.IsEnum)
        {
            var values = Enum.GetValues(underlying);
            foreach (var v in values)
            {
                if (Convert.ToInt64(v, CultureInfo.InvariantCulture) != 0)
                    return v;
            }
            return values.GetValue(0)!;
        }

        throw new NotSupportedException($"Неподдерживаемый тип для генерации: {type.FullName}");
    }

    private static void AssertNotDefault(object instance)
    {
        var type = instance.GetType();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(instance);
            value.Should().NotBeNull($"Свойство {type.Name}.{prop.Name} не должно быть null");

            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            if (propType == typeof(Guid))
            {
                ((Guid)value).Should().NotBe(Guid.Empty, $"Свойство {type.Name}.{prop.Name} не должно быть Guid.Empty");
            }
            else if (propType == typeof(string))
            {
                ((string)value).Should().NotBeNullOrWhiteSpace($"Свойство {type.Name}.{prop.Name} должно быть непустой строкой");
            }
            else if (propType == typeof(int))
            {
                ((int)value).Should().NotBe(0, $"Свойство {type.Name}.{prop.Name} не должно быть 0");
            }
            else if (propType == typeof(long))
            {
                ((long)value).Should().NotBe(0L, $"Свойство {type.Name}.{prop.Name} не должно быть 0L");
            }
            else if (propType == typeof(decimal))
            {
                ((decimal)value).Should().NotBe(0m, $"Свойство {type.Name}.{prop.Name} не должно быть 0m");
            }
            else if (propType == typeof(bool))
            {
                ((bool)value).Should().BeTrue($"Свойство {type.Name}.{prop.Name} должно быть true");
            }
            else if (propType == typeof(DateTime))
            {
                ((DateTime)value).Should().NotBe(default(DateTime), $"Свойство {type.Name}.{prop.Name} не должно быть default(DateTime)");
            }
            else if (propType == typeof(DateTimeOffset))
            {
                ((DateTimeOffset)value).Should().NotBe(default(DateTimeOffset), $"Свойство {type.Name}.{prop.Name} не должно быть default(DateTimeOffset)");
            }
            else if (propType.IsEnum)
            {
                Convert.ToInt64(value, CultureInfo.InvariantCulture).Should().NotBe(0, $"Свойство {type.Name}.{prop.Name} не должно быть 0 (default enum)");
            }
        }
    }
}

