using AwesomeAssertions;
using AwesomeAssertions.Specialized;
using Users.Domain.Entities;
using Users.Domain.Enums;
using Users.Domain.Exceptions;

namespace Users.Tests.Domain;

public class UserTests
{
    [Fact]
    public void Create_WithEmptyPassword_ShouldThrowsValidationException()
    {
        // Arrange
        string userName = "UserName";
        string userPassword = "";

        // Act
        Action act = () => User.Create(userName, userPassword);

        // Assert
        ExceptionAssertions<ValidationException> exception = act.Should().Throw<ValidationException>();
        exception.Which.ErrorCode.Should().Be("Domain.ValidationFailed");
        exception.Which.ValidationErrors.Should().ContainKey("passwordHash");
    }


    [Fact]
    public void Create_WithEmptyUserName_ShouldThrowsValidationException()
    {
        // Arrange
        string userName = "";
        string userPassword = "Password123";

        // Act
        Action act = () => User.Create(userName, userPassword);

        // Assert
        ExceptionAssertions<ValidationException> exception = act.Should().Throw<ValidationException>();
        exception.Which.ErrorCode.Should().Be("Domain.ValidationFailed");
        exception.Which.ValidationErrors.Should().ContainKey("userName");
    }

    [Fact]
    public void Create_WithValidDataWithRoleIsAdmin_ShouldReturnAdminRole()
    {
        // Arrange
        string userName = "UserName";
        string userPassword = "Password123";

        // Act
        User user = User.Create(userName, userPassword, "Admin");

        // Assert
        user.Should().NotBeNull();
        user.Role.Should().Be(RoleName.Admin);
    }

    [Fact]
    public void Create_WithValidDataWithRoleIsUser_ShouldReturnUserRole()
    {
        // Arrange
        string userName = "UserName";
        string userPassword = "Password123";

        // Act
        User user = User.Create(userName, userPassword);

        // Assert
        user.Should().NotBeNull();
        user.Role.Should().Be(RoleName.User);
    }

    [Fact]
    public void Create_WithValidDataWithoutRole_ShouldReturnUserRole()
    {
        // Arrange
        string userName = "UserName";
        string userPassword = "Password123";

        // Act
        User user = User.Create(userName, userPassword);

        // Assert
        user.Should().NotBeNull();
        user.Role.Should().Be(RoleName.User);
    }

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        string userName = "UserName";
        string userPassword = "Password123";

        // Act
        User user = User.Create(userName, userPassword);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.UserName.Should().Be(userName);
        user.PasswordHash.Should().NotBeNull();
    }
}
