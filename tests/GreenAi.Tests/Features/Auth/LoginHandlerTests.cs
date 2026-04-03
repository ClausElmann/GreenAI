using GreenAi.Api.Features.Auth.Login;
using GreenAi.Api.Features.Auth.SelectProfile;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GreenAi.Tests.Features.Auth;

public sealed class LoginHandlerTests
{
    private static JwtTokenService CreateJwtService() =>
        new(Options.Create(new JwtOptions
        {
            SecretKey = "test-secret-key-that-is-long-enough-for-hmac256",
            Issuer = "greenai-test",
            Audience = "greenai-test",
            AccessTokenExpiryMinutes = 60,
            RefreshTokenExpiryDays = 30
        }));

    private static LoginUserRecord ValidUser()
    {
        var (hash, salt) = PasswordHasher.Hash("correct-password");
        return new LoginUserRecord(
            Id: 1,
            Email: "user@example.com",
            PasswordHash: hash,
            PasswordSalt: salt,
            FailedLoginCount: 0,
            IsLockedOut: false);
    }

    /// <summary>
    /// Creates an IDbSession substitute whose ExecuteInTransactionAsync invokes the work func directly.
    /// Unit tests don't need real transactions — this ensures the work lambda still executes.
    /// </summary>
    private static IDbSession CreateDbSession()
    {
        var db = Substitute.For<IDbSession>();
        db.ExecuteInTransactionAsync(Arg.Any<Func<Task>>())
          .Returns(ci => ci.Arg<Func<Task>>()());
        return db;
    }

    [Fact]
    public async Task Handle_ValidCredentialsSingleProfile_ReturnsToken()
    {
        var repository = Substitute.For<ILoginRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        var jwt = CreateJwtService();
        var user = ValidUser();

        repository.FindByEmailAsync("user@example.com").Returns(user);
        repository.GetMembershipsAsync(new UserId(user.Id))
            .Returns(new[] { new UserMembershipRecord(10, "Test Customer", 1) });
        repository.GetProfilesAsync(new UserId(user.Id), new CustomerId(10))
            .Returns((IReadOnlyCollection<ProfileRecord>)[new ProfileRecord(ProfileId: 5, DisplayName: "Main")]);

        var handler = new LoginHandler(repository, jwt, tokenWriter, CreateDbSession());
        var result = await handler.Handle(new LoginCommand("user@example.com", "correct-password"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.AccessToken);
        Assert.NotEmpty(result.Value.RefreshToken);
        Assert.False(result.Value.NeedsCustomerSelection);
        Assert.False(result.Value.NeedsProfileSelection);
    }

    [Fact]
    public async Task Handle_ValidCredentialsSingleProfile_IssuesTokenWithRealProfileId()
    {
        // RULE: ProfileId(0) must never be issued. Handler must pass the real ProfileId row.
        var repository = Substitute.For<ILoginRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        var jwt = CreateJwtService();
        var user = ValidUser();

        repository.FindByEmailAsync("user@example.com").Returns(user);
        repository.GetMembershipsAsync(new UserId(user.Id))
            .Returns(new[] { new UserMembershipRecord(10, "Test Customer", 1) });
        repository.GetProfilesAsync(new UserId(user.Id), new CustomerId(10))
            .Returns((IReadOnlyCollection<ProfileRecord>)[new ProfileRecord(ProfileId: 7, DisplayName: "Profile A")]);

        var handler = new LoginHandler(repository, jwt, tokenWriter, CreateDbSession());
        await handler.Handle(new LoginCommand("user@example.com", "correct-password"), TestContext.Current.CancellationToken);

        await tokenWriter.Received(1).SaveAsync(
            new UserId(user.Id),
            new CustomerId(10),
            new ProfileId(7),  // must be the real profile id — never 0
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            1);
    }

    [Fact]
    public async Task Handle_ValidCredentialsMultipleProfiles_ReturnsRequiresProfileSelection()
    {
        var repository = Substitute.For<ILoginRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        var jwt = CreateJwtService();
        var user = ValidUser();

        repository.FindByEmailAsync("user@example.com").Returns(user);
        repository.GetMembershipsAsync(new UserId(user.Id))
            .Returns(new[] { new UserMembershipRecord(10, "Test Customer", 1) });
        repository.GetProfilesAsync(new UserId(user.Id), new CustomerId(10))
            .Returns((IReadOnlyCollection<ProfileRecord>)[
                new ProfileRecord(ProfileId: 1, DisplayName: "Alpha"),
                new ProfileRecord(ProfileId: 2, DisplayName: "Beta")]);

        var handler = new LoginHandler(repository, jwt, tokenWriter, CreateDbSession());
        var result = await handler.Handle(new LoginCommand("user@example.com", "correct-password"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.NeedsProfileSelection);
        Assert.Equal(2, result.Value.AvailableProfiles!.Count);
        await tokenWriter.DidNotReceive().SaveAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<ProfileId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>());
    }

    [Fact]
    public async Task Handle_ValidCredentialsNoProfile_ReturnsError()
    {
        var repository = Substitute.For<ILoginRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        var jwt = CreateJwtService();
        var user = ValidUser();

        repository.FindByEmailAsync("user@example.com").Returns(user);
        repository.GetMembershipsAsync(new UserId(user.Id))
            .Returns(new[] { new UserMembershipRecord(10, "Test Customer", 1) });
        repository.GetProfilesAsync(new UserId(user.Id), new CustomerId(10))
            .Returns((IReadOnlyCollection<ProfileRecord>)[]);

        var handler = new LoginHandler(repository, jwt, tokenWriter, CreateDbSession());
        var result = await handler.Handle(new LoginCommand("user@example.com", "correct-password"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("PROFILE_NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsInvalidCredentials()
    {
        var repository = Substitute.For<ILoginRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        var jwt = CreateJwtService();

        repository.FindByEmailAsync(Arg.Any<string>()).Returns((LoginUserRecord?)null);

        var handler = new LoginHandler(repository, jwt, tokenWriter, CreateDbSession());
        var result = await handler.Handle(new LoginCommand("nobody@example.com", "any"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDENTIALS", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentialsAndRecordsFailure()
    {
        var repository = Substitute.For<ILoginRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        var jwt = CreateJwtService();
        var user = ValidUser();

        repository.FindByEmailAsync("user@example.com").Returns(user);
        repository.RecordFailedLoginAsync(Arg.Any<UserId>()).Returns(Task.CompletedTask);

        var handler = new LoginHandler(repository, jwt, tokenWriter, CreateDbSession());
        var result = await handler.Handle(new LoginCommand("user@example.com", "wrong-password"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDENTIALS", result.Error!.Code);
        await repository.Received(1).RecordFailedLoginAsync(new UserId(user.Id));
    }

    [Fact]
    public async Task Handle_LockedOutAccount_ReturnsAccountLocked()
    {
        var repository = Substitute.For<ILoginRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        var jwt = CreateJwtService();
        var (hash, salt) = PasswordHasher.Hash("correct-password");
        var lockedUser = new LoginUserRecord(1, "user@example.com", hash, salt, 10, IsLockedOut: true);

        repository.FindByEmailAsync("user@example.com").Returns(lockedUser);

        var handler = new LoginHandler(repository, jwt, tokenWriter, CreateDbSession());
        var result = await handler.Handle(new LoginCommand("user@example.com", "correct-password"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_LOCKED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ValidCredentialsWithPriorFailures_ResetsFailedCount()
    {
        var repository = Substitute.For<ILoginRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        var jwt = CreateJwtService();
        var (hash, salt) = PasswordHasher.Hash("correct-password");
        var userWithFailures = new LoginUserRecord(1, "user@example.com", hash, salt, FailedLoginCount: 3, IsLockedOut: false);

        repository.FindByEmailAsync("user@example.com").Returns(userWithFailures);
        repository.GetMembershipsAsync(new UserId(userWithFailures.Id))
            .Returns(new[] { new UserMembershipRecord(10, "Test Customer", 1) });
        repository.GetProfilesAsync(new UserId(userWithFailures.Id), new CustomerId(10))
            .Returns((IReadOnlyCollection<ProfileRecord>)[new ProfileRecord(ProfileId: 5, DisplayName: "Main")]);
        repository.ResetFailedLoginAsync(Arg.Any<UserId>()).Returns(Task.CompletedTask);

        var handler = new LoginHandler(repository, jwt, tokenWriter, CreateDbSession());
        var result = await handler.Handle(new LoginCommand("user@example.com", "correct-password"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        await repository.Received(1).ResetFailedLoginAsync(new UserId(userWithFailures.Id));
    }

    [Fact]
    public async Task Handle_ValidCredentialsZeroMemberships_ReturnsAccountHasNoTenant()
    {
        var repository = Substitute.For<ILoginRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        var jwt = CreateJwtService();
        var user = ValidUser();

        repository.FindByEmailAsync("user@example.com").Returns(user);
        repository.GetMembershipsAsync(new UserId(user.Id))
            .Returns(Array.Empty<UserMembershipRecord>());

        var handler = new LoginHandler(repository, jwt, tokenWriter, CreateDbSession());
        var result = await handler.Handle(new LoginCommand("user@example.com", "correct-password"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_HAS_NO_TENANT", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ValidCredentialsMultipleMemberships_ReturnsRequiresCustomerSelection()
    {
        var repository = Substitute.For<ILoginRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        var jwt = CreateJwtService();
        var user = ValidUser();

        repository.FindByEmailAsync("user@example.com").Returns(user);
        repository.GetMembershipsAsync(new UserId(user.Id))
            .Returns(new[]
            {
                new UserMembershipRecord(10, "Customer A", 1),
                new UserMembershipRecord(20, "Customer B", 1),
            });

        var handler = new LoginHandler(repository, jwt, tokenWriter, CreateDbSession());
        var result = await handler.Handle(new LoginCommand("user@example.com", "correct-password"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.NeedsCustomerSelection);
        await tokenWriter.DidNotReceive().SaveAsync(
            Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<ProfileId>(),
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>());
    }
}
