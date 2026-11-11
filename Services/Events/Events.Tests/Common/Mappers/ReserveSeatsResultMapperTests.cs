using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using Events.Application.Common.Mappers;
using Events.Domain.Enums;
using Xunit;

namespace Events.Tests.Common.Mappers;

public class ReserveSeatsResultMapperTests
{
    [Theory]
    [InlineData(ReserveSeatsResult.EventInactive, ValidationFailureReason.EventNotFound)]
    [InlineData(ReserveSeatsResult.InsufficientSeats, ValidationFailureReason.SeatsNotAvailable)]
    public void ToValidationFailureReason_WhenValidRejection_ShouldMapCorrectly(
        ReserveSeatsResult result,
        ValidationFailureReason expected)
    {
        // Act
        var mapped = result.ToValidationFailureReason();

        // Assert
        mapped.Should().Be(expected);
    }

    [Theory]
    [InlineData(ReserveSeatsResult.Reserved)]
    [InlineData(ReserveSeatsResult.None)]
    [InlineData((ReserveSeatsResult)999)]
    public void ToValidationFailureReason_WhenInvalidOrSuccess_ShouldThrowArgumentOutOfRangeException(
        ReserveSeatsResult result)
    {
        // Act
        Action act = () => result.ToValidationFailureReason();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToValidationFailureReason_MustHandleAllEnumValues()
    {
        // Arrange & Act & Assert
        // Гарантирует полноту: при добавлении любого нового исхода в ReserveSeatsResult
        // разработчик обязан явно добавить его обработку в маппер и этот тест.
        var allValues = Enum.GetValues<ReserveSeatsResult>();
        allValues.Should().HaveCount(4, "В enum ReserveSeatsResult ожидаются ровно 4 значения: None, Reserved, EventInactive, InsufficientSeats.");

        foreach (var val in allValues)
        {
            if (val is ReserveSeatsResult.Reserved or ReserveSeatsResult.None)
            {
                Action act = () => val.ToValidationFailureReason();
                act.Should().Throw<ArgumentOutOfRangeException>();
            }
            else
            {
                Action act = () => val.ToValidationFailureReason();
                act.Should().NotThrow();
            }
        }
    }
}
