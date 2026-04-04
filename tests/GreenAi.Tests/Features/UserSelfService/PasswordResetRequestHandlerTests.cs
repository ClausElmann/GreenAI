using GreenAi.Api.Features.UserSelfService.PasswordReset;
using GreenAi.Api.SharedKernel.Email;
using GreenAi.Api.SharedKernel.Settings;
using NSubstitute;

namespace GreenAi.Tests.Features.UserSelfService;

public sealed class PasswordResetRequestHandlerTests
{
    private static PasswordResetRequestHandler CreateHandler(
        IPasswordResetRequestRepository? repository = null,
        IEmailService?                   emailService = null,
        IApplicationSettingService?      settings = null)
    {
        if (settings is null)
        {
            var s = Substitute.For<IApplicationSettingService>();
            s.GetAsync(AppSetting.PasswordResetTokenTtlMinutes, Arg.Any<string?>())
             .Returns("60");
            s.GetAsync(AppSetting.PasswordResetBaseUrl, Arg.Any<string?>())
             .Returns("https://localhost");
            settings = s;
        }

        return new PasswordResetRequestHandler(
            repository   ?? Substitute.For<IPasswordResetRequestRepository>(),
            emailService ?? Substitute.For<IEmailService>(),
            settings);
    }

    // ===================================================================
    // User not found — always returns success (anti-enumeration)
    // ===================================================================

    [Fact]
    public async Task Handle_EmailNotFound_ReturnsSuccess()
    {
        var repository = Substitute.For<IPasswordResetRequestRepository>();
        repository.FindUserByEmailAsync(Arg.Any<string>())
                  .Returns((PasswordResetUserRecord?)null);

        var result = await CreateHandler(repository).Handle(
            new PasswordResetRequestCommand("nobody@test.local"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains("If an account exists", result.Value!.Message);
    }

    [Fact]
    public async Task Handle_EmailNotFound_DoesNotInsertToken()
    {
        var repository = Substitute.For<IPasswordResetRequestRepository>();
        repository.FindUserByEmailAsync(Arg.Any<string>())
                  .Returns((PasswordResetUserRecord?)null);

        await CreateHandler(repository).Handle(
            new PasswordResetRequestCommand("nobody@test.local"),
            TestContext.Current.CancellationToken);

        await repository.DidNotReceive().InsertTokenAsync(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public async Task Handle_EmailNotFound_DoesNotSendEmail()
    {
        var repository   = Substitute.For<IPasswordResetRequestRepository>();
        var emailService = Substitute.For<IEmailService>();
        repository.FindUserByEmailAsync(Arg.Any<string>())
                  .Returns((PasswordResetUserRecord?)null);

        await CreateHandler(repository, emailService).Handle(
            new PasswordResetRequestCommand("nobody@test.local"),
            TestContext.Current.CancellationToken);

        await emailService.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    // ===================================================================
    // User found — token generated, stored, email sent
    // ===================================================================

    [Fact]
    public async Task Handle_UserFound_ReturnsSuccess()
    {
        var repository = Substitute.For<IPasswordResetRequestRepository>();
        repository.FindUserByEmailAsync("user@test.local")
                  .Returns(new PasswordResetUserRecord(UserId: 42, Email: "user@test.local"));

        var result = await CreateHandler(repository).Handle(
            new PasswordResetRequestCommand("user@test.local"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_UserFound_InsertsToken()
    {
        var repository = Substitute.For<IPasswordResetRequestRepository>();
        repository.FindUserByEmailAsync("user@test.local")
                  .Returns(new PasswordResetUserRecord(UserId: 42, Email: "user@test.local"));

        await CreateHandler(repository).Handle(
            new PasswordResetRequestCommand("user@test.local"),
            TestContext.Current.CancellationToken);

        await repository.Received(1).InsertTokenAsync(
            42,
            Arg.Is<string>(t => t.Length == 64),
            Arg.Is<DateTimeOffset>(e => e > DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task Handle_UserFound_SendsEmail()
    {
        var repository   = Substitute.For<IPasswordResetRequestRepository>();
        var emailService = Substitute.For<IEmailService>();
        repository.FindUserByEmailAsync("user@test.local")
                  .Returns(new PasswordResetUserRecord(UserId: 42, Email: "user@test.local"));

        await CreateHandler(repository, emailService).Handle(
            new PasswordResetRequestCommand("user@test.local"),
            TestContext.Current.CancellationToken);

        await emailService.Received(1).SendAsync(
            "user@test.local",
            "password-reset",
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d.ContainsKey("link") && d.ContainsKey("token") && d.ContainsKey("ttl")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserFound_TokenIsUnique64CharHex()
    {
        var repository = Substitute.For<IPasswordResetRequestRepository>();
        string? capturedToken = null;
        repository.FindUserByEmailAsync("user@test.local")
                  .Returns(new PasswordResetUserRecord(UserId: 42, Email: "user@test.local"));
        await repository.InsertTokenAsync(
            Arg.Any<int>(),
            Arg.Do<string>(t => capturedToken = t),
            Arg.Any<DateTimeOffset>());

        await CreateHandler(repository).Handle(
            new PasswordResetRequestCommand("user@test.local"),
            TestContext.Current.CancellationToken);

        Assert.NotNull(capturedToken);
        Assert.Equal(64, capturedToken!.Length);
        Assert.Matches("^[0-9a-f]{64}$", capturedToken);
    }
}
