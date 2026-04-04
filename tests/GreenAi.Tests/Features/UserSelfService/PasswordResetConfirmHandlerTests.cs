using GreenAi.Api.Features.UserSelfService.PasswordReset;
using NSubstitute;

namespace GreenAi.Tests.Features.UserSelfService;

public sealed class PasswordResetConfirmHandlerTests
{
    private static PasswordResetConfirmHandler CreateHandler(
        IPasswordResetConfirmRepository? repository = null)
        => new(repository ?? Substitute.For<IPasswordResetConfirmRepository>());

    private static PasswordResetTokenRecord ValidToken() =>
        new(Id: 1, UserId: 42, Token: new string('a', 64), ExpiresAt: DateTimeOffset.UtcNow.AddHours(1));

    // ===================================================================
    // Token not found / expired
    // ===================================================================

    [Fact]
    public async Task Handle_TokenNotFound_ReturnsInvalidToken()
    {
        var repository = Substitute.For<IPasswordResetConfirmRepository>();
        repository.FindTokenAsync(Arg.Any<string>())
                  .Returns((PasswordResetTokenRecord?)null);

        var result = await CreateHandler(repository).Handle(
            new PasswordResetConfirmCommand(new string('x', 64), "newPass123", "newPass123"),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_TOKEN", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_TokenNotFound_DoesNotCallConfirmReset()
    {
        var repository = Substitute.For<IPasswordResetConfirmRepository>();
        repository.FindTokenAsync(Arg.Any<string>())
                  .Returns((PasswordResetTokenRecord?)null);

        await CreateHandler(repository).Handle(
            new PasswordResetConfirmCommand(new string('x', 64), "newPass123", "newPass123"),
            TestContext.Current.CancellationToken);

        await repository.DidNotReceive().ConfirmResetAsync(
            Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string>(), Arg.Any<string>());
    }

    // ===================================================================
    // Valid token
    // ===================================================================

    [Fact]
    public async Task Handle_ValidToken_ReturnsSuccess()
    {
        var repository = Substitute.For<IPasswordResetConfirmRepository>();
        repository.FindTokenAsync(Arg.Any<string>()).Returns(ValidToken());

        var result = await CreateHandler(repository).Handle(
            new PasswordResetConfirmCommand(new string('a', 64), "newPass123", "newPass123"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Password reset successfully.", result.Value!.Message);
    }

    [Fact]
    public async Task Handle_ValidToken_CallsConfirmResetWithTokenIdAndUserId()
    {
        var repository = Substitute.For<IPasswordResetConfirmRepository>();
        var token      = ValidToken();
        repository.FindTokenAsync(Arg.Any<string>()).Returns(token);

        await CreateHandler(repository).Handle(
            new PasswordResetConfirmCommand(new string('a', 64), "newPass123", "newPass123"),
            TestContext.Current.CancellationToken);

        await repository.Received(1).ConfirmResetAsync(
            token.Id,
            token.UserId,
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_ValidToken_PasswordHashIsNotPlaintext()
    {
        var repository = Substitute.For<IPasswordResetConfirmRepository>();
        repository.FindTokenAsync(Arg.Any<string>()).Returns(ValidToken());

        string? capturedHash = null;
        await repository.ConfirmResetAsync(
            Arg.Any<int>(), Arg.Any<int>(),
            Arg.Do<string>(h => capturedHash = h), Arg.Any<string>());

        await CreateHandler(repository).Handle(
            new PasswordResetConfirmCommand(new string('a', 64), "newPass123", "newPass123"),
            TestContext.Current.CancellationToken);

        Assert.NotNull(capturedHash);
        Assert.NotEqual("newPass123", capturedHash); // must be hashed, not plaintext
    }
}
