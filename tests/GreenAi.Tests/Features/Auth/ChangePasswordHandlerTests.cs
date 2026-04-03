using GreenAi.Api.Features.Auth.ChangePassword;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using NSubstitute;

namespace GreenAi.Tests.Features.Auth;

public sealed class ChangePasswordHandlerTests
{
    private static ICurrentUser CurrentUser(int userId = 42)
    {
        var mock = Substitute.For<ICurrentUser>();
        mock.UserId.Returns(new UserId(userId));
        return mock;
    }

    private static ChangePasswordUserRecord ValidRecord(bool isLockedOut = false)
    {
        var (hash, salt) = PasswordHasher.Hash("correct-password");
        return new ChangePasswordUserRecord(Id: 42, PasswordHash: hash, PasswordSalt: salt, IsLockedOut: isLockedOut);
    }

    private ChangePasswordHandler CreateHandler(
        IChangePasswordRepository? repository = null,
        ICurrentUser? currentUser = null) =>
        new(
            repository ?? Substitute.For<IChangePasswordRepository>(),
            currentUser ?? CurrentUser());

    // ===================================================================
    // Happy path
    // ===================================================================

    [Fact]
    public async Task Handle_ValidCurrentPassword_ReturnsSuccess()
    {
        // Arrange
        var repository = Substitute.For<IChangePasswordRepository>();
        var currentUser = CurrentUser(42);
        repository.FindByUserIdAsync(new UserId(42)).Returns(ValidRecord());

        var handler = new ChangePasswordHandler(repository, currentUser);

        var command = new ChangePasswordCommand("correct-password", "newPass123", "newPass123");

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Password changed successfully.", result.Value!.Message);
    }

    [Fact]
    public async Task Handle_ValidCurrentPassword_CallsUpdatePasswordAsync()
    {
        // Arrange
        var repository = Substitute.For<IChangePasswordRepository>();
        var currentUser = CurrentUser(42);
        repository.FindByUserIdAsync(new UserId(42)).Returns(ValidRecord());

        var handler = new ChangePasswordHandler(repository, currentUser);

        // Act
        await handler.Handle(
            new ChangePasswordCommand("correct-password", "newPass123", "newPass123"),
            TestContext.Current.CancellationToken);

        // Assert — UpdatePasswordAsync must be called exactly once
        await repository.Received(1).UpdatePasswordAsync(
            new UserId(42),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    // ===================================================================
    // User not found
    // ===================================================================

    [Fact]
    public async Task Handle_UserNotFound_ReturnsUnauthorizedError()
    {
        // Arrange
        var repository = Substitute.For<IChangePasswordRepository>();
        repository.FindByUserIdAsync(Arg.Any<UserId>()).Returns((ChangePasswordUserRecord?)null);

        // Act
        var result = await CreateHandler(repository).Handle(
            new ChangePasswordCommand("any", "newPass123", "newPass123"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_UserNotFound_DoesNotCallUpdatePassword()
    {
        // Arrange
        var repository = Substitute.For<IChangePasswordRepository>();
        repository.FindByUserIdAsync(Arg.Any<UserId>()).Returns((ChangePasswordUserRecord?)null);

        // Act
        await CreateHandler(repository).Handle(
            new ChangePasswordCommand("any", "newPass123", "newPass123"),
            TestContext.Current.CancellationToken);

        // Assert
        await repository.DidNotReceive().UpdatePasswordAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // ===================================================================
    // Account locked
    // ===================================================================

    [Fact]
    public async Task Handle_AccountLockedOut_ReturnsAccountLockedError()
    {
        // Arrange
        var repository = Substitute.For<IChangePasswordRepository>();
        repository.FindByUserIdAsync(Arg.Any<UserId>()).Returns(ValidRecord(isLockedOut: true));

        // Act
        var result = await CreateHandler(repository).Handle(
            new ChangePasswordCommand("correct-password", "newPass123", "newPass123"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_LOCKED", result.Error!.Code);
    }

    // ===================================================================
    // Wrong current password
    // ===================================================================

    [Fact]
    public async Task Handle_WrongCurrentPassword_ReturnsInvalidCredentialsError()
    {
        // Arrange
        var repository = Substitute.For<IChangePasswordRepository>();
        repository.FindByUserIdAsync(Arg.Any<UserId>()).Returns(ValidRecord());

        // Act
        var result = await CreateHandler(repository).Handle(
            new ChangePasswordCommand("wrong-password", "newPass123", "newPass123"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDENTIALS", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_DoesNotCallUpdatePassword()
    {
        // Arrange
        var repository = Substitute.For<IChangePasswordRepository>();
        repository.FindByUserIdAsync(Arg.Any<UserId>()).Returns(ValidRecord());

        // Act
        await CreateHandler(repository).Handle(
            new ChangePasswordCommand("wrong-password", "newPass123", "newPass123"),
            TestContext.Current.CancellationToken);

        // Assert
        await repository.DidNotReceive().UpdatePasswordAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
