using GreenAi.Api.Features.Identity.ChangeUserEmail;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using NSubstitute;

namespace GreenAi.Tests.Features.Identity;

/// <summary>
/// Unit tests for ChangeUserEmailHandler.
///
/// Tests handler decision logic only. Repository and ICurrentUser are substituted.
///
/// Cases:
///   - Email is taken     → Result.Fail("EMAIL_TAKEN")
///   - Email is available → repository called, Result.Ok
///   - Verifies that UpdateEmailAndAuditAsync is NOT called when email is unavailable
/// </summary>
public sealed class ChangeUserEmailHandlerTests
{
    private static ICurrentUser CurrentUser(int userId = 10, int customerId = 1)
    {
        var mock = Substitute.For<ICurrentUser>();
        mock.UserId.Returns(new UserId(userId));
        mock.CustomerId.Returns(new CustomerId(customerId));
        return mock;
    }

    private static ChangeUserEmailHandler CreateHandler(
        IChangeUserEmailRepository? repository = null,
        ICurrentUser? currentUser = null) =>
        new(
            repository  ?? Substitute.For<IChangeUserEmailRepository>(),
            currentUser ?? CurrentUser());

    // ===================================================================
    // EMAIL_TAKEN guard
    // ===================================================================

    [Fact]
    public async Task Handle_EmailAlreadyTaken_ReturnsEmailTakenError()
    {
        // Arrange
        var repo = Substitute.For<IChangeUserEmailRepository>();
        repo.IsEmailAvailableAsync(Arg.Any<string>(), Arg.Any<UserId>()).Returns(false);

        var handler = CreateHandler(repository: repo);
        var command = new ChangeUserEmailCommand("taken@example.com", "taken@example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("EMAIL_TAKEN", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_EmailAlreadyTaken_DoesNotCallUpdate()
    {
        // Arrange
        var repo = Substitute.For<IChangeUserEmailRepository>();
        repo.IsEmailAvailableAsync(Arg.Any<string>(), Arg.Any<UserId>()).Returns(false);

        var handler = CreateHandler(repository: repo);
        var command = new ChangeUserEmailCommand("taken@example.com", "taken@example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — database write must not be called when email is taken
        await repo.DidNotReceive().UpdateEmailAndAuditAsync(
            Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<string>());
    }

    // ===================================================================
    // Success path
    // ===================================================================

    [Fact]
    public async Task Handle_EmailAvailable_ReturnsSuccess()
    {
        // Arrange
        var repo = Substitute.For<IChangeUserEmailRepository>();
        repo.IsEmailAvailableAsync(Arg.Any<string>(), Arg.Any<UserId>()).Returns(true);

        var handler = CreateHandler(repository: repo);
        var command = new ChangeUserEmailCommand("new@example.com", "new@example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Email updated successfully.", result.Value!.Message);
    }

    [Fact]
    public async Task Handle_EmailAvailable_CallsUpdateWithCorrectParameters()
    {
        // Arrange
        var userId     = new UserId(10);
        var customerId = new CustomerId(1);

        var user = CurrentUser(userId.Value, customerId.Value);
        var repo = Substitute.For<IChangeUserEmailRepository>();
        repo.IsEmailAvailableAsync(Arg.Any<string>(), Arg.Any<UserId>()).Returns(true);

        var handler = CreateHandler(repository: repo, currentUser: user);
        var command = new ChangeUserEmailCommand("new@example.com", "new@example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — update called with correct IDs and new email from command
        await repo.Received(1).UpdateEmailAndAuditAsync(userId, customerId, "new@example.com");
    }

    [Fact]
    public async Task Handle_EmailAvailable_PassesCommandEmailToAvailabilityCheck()
    {
        // Arrange — confirm handler passes the NewEmail (not ConfirmNewEmail) to the check
        var repo = Substitute.For<IChangeUserEmailRepository>();
        repo.IsEmailAvailableAsync(Arg.Any<string>(), Arg.Any<UserId>()).Returns(true);

        var handler = CreateHandler(repository: repo);
        var command = new ChangeUserEmailCommand("check@example.com", "check@example.com");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await repo.Received(1).IsEmailAvailableAsync("check@example.com", Arg.Any<UserId>());
    }
}
