using GreenAi.Api.Features.Auth.SelectProfile;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GreenAi.Tests.Features.Auth;

public sealed class SelectProfileHandlerTests
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

    private static ICurrentUser CreateCurrentUser(int userId = 1, int customerId = 10, int languageId = 1, string email = "user@example.com")
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(new UserId(userId));
        user.CustomerId.Returns(new CustomerId(customerId));
        user.LanguageId.Returns(languageId);
        user.Email.Returns(email);
        return user;
    }

    private static SelectProfileHandler CreateHandler(
        ISelectProfileRepository repository,
        ICurrentUser? currentUser = null,
        IRefreshTokenWriter? tokenWriter = null)
        => new(repository, CreateJwtService(), currentUser ?? CreateCurrentUser(), tokenWriter ?? Substitute.For<IRefreshTokenWriter>());

    // ===================================================================
    // Auto-resolve — single profile
    // ===================================================================

    [Fact]
    public async Task Handle_SingleProfile_AutoResolvesAndReturnsToken()
    {
        var repository = Substitute.For<ISelectProfileRepository>();
        repository.GetAvailableProfilesAsync(new UserId(1), new CustomerId(10))
            .Returns((IReadOnlyCollection<ProfileRecord>)[new ProfileRecord(ProfileId: 5, DisplayName: "Main")]);

        var result = await CreateHandler(repository).Handle(new SelectProfileCommand(null), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!.AccessToken);
        Assert.False(result.Value.NeedsProfileSelection);
    }

    [Fact]
    public async Task Handle_SingleProfile_SavesRefreshTokenWithRealProfileId()
    {
        // RULE: ProfileId(0) must never be issued. Handler must pass the real Profiles.Id.
        var repository = Substitute.For<ISelectProfileRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        var currentUser = CreateCurrentUser(userId: 1, customerId: 10, languageId: 2);
        repository.GetAvailableProfilesAsync(new UserId(1), new CustomerId(10))
            .Returns((IReadOnlyCollection<ProfileRecord>)[new ProfileRecord(ProfileId: 7, DisplayName: "Profile A")]);

        await CreateHandler(repository, currentUser, tokenWriter).Handle(new SelectProfileCommand(null), TestContext.Current.CancellationToken);

        await tokenWriter.Received(1).SaveAsync(
            new UserId(1),
            new CustomerId(10),
            new ProfileId(7),  // must be the real profile id — never 0
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            2); // languageId from ICurrentUser
    }

    // ===================================================================
    // Explicit selection — multiple profiles
    // ===================================================================

    [Fact]
    public async Task Handle_ValidExplicitProfileId_ReturnsToken()
    {
        var repository = Substitute.For<ISelectProfileRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        repository.GetAvailableProfilesAsync(new UserId(1), new CustomerId(10))
            .Returns((IReadOnlyCollection<ProfileRecord>)[
                new ProfileRecord(ProfileId: 1, DisplayName: "Alpha"),
                new ProfileRecord(ProfileId: 2, DisplayName: "Beta")]);

        var result = await CreateHandler(repository, tokenWriter: tokenWriter).Handle(new SelectProfileCommand(2), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!.AccessToken);
        await tokenWriter.Received(1).SaveAsync(
            Arg.Any<UserId>(),
            Arg.Any<CustomerId>(),
            new ProfileId(2),
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<int>());
    }

    // ===================================================================
    // Multi-profile without selection — requires selection
    // ===================================================================

    [Fact]
    public async Task Handle_MultipleProfilesNoSelection_ReturnsRequiresProfileSelection()
    {
        var repository = Substitute.For<ISelectProfileRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        repository.GetAvailableProfilesAsync(new UserId(1), new CustomerId(10))
            .Returns((IReadOnlyCollection<ProfileRecord>)[
                new ProfileRecord(ProfileId: 1, DisplayName: "Alpha"),
                new ProfileRecord(ProfileId: 2, DisplayName: "Beta")]);

        var result = await CreateHandler(repository, tokenWriter: tokenWriter).Handle(new SelectProfileCommand(null), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.NeedsProfileSelection);
        Assert.Equal(2, result.Value.AvailableProfiles!.Count);
        await tokenWriter.DidNotReceive().SaveAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<ProfileId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>());
    }

    // ===================================================================
    // Error paths
    // ===================================================================

    [Fact]
    public async Task Handle_InvalidProfileId_ReturnsAccessDenied()
    {
        var repository = Substitute.For<ISelectProfileRepository>();
        repository.GetAvailableProfilesAsync(new UserId(1), new CustomerId(10))
            .Returns((IReadOnlyCollection<ProfileRecord>)[new ProfileRecord(ProfileId: 5, DisplayName: "Only Profile")]);

        // Client supplies a ProfileId that does not belong to this user+customer
        var result = await CreateHandler(repository).Handle(new SelectProfileCommand(99), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("PROFILE_ACCESS_DENIED", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_InaccessibleProfileId_DoesNotSaveRefreshToken()
    {
        var repository = Substitute.For<ISelectProfileRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        repository.GetAvailableProfilesAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>())
            .Returns((IReadOnlyCollection<ProfileRecord>)[new ProfileRecord(ProfileId: 5, DisplayName: "Only Profile")]);

        await CreateHandler(repository, tokenWriter: tokenWriter).Handle(new SelectProfileCommand(999), TestContext.Current.CancellationToken);

        await tokenWriter.DidNotReceive().SaveAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<ProfileId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>());
    }

    [Fact]
    public async Task Handle_NoProfiles_ReturnsError()
    {
        var repository = Substitute.For<ISelectProfileRepository>();
        repository.GetAvailableProfilesAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>())
            .Returns((IReadOnlyCollection<ProfileRecord>)[]);

        var result = await CreateHandler(repository).Handle(new SelectProfileCommand(null), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("PROFILE_NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_UsesUserIdAndCustomerIdFromCurrentUser()
    {
        // RULE: UserId and CustomerId must originate from ICurrentUser (JWT claims) — never from client input.
        var repository = Substitute.For<ISelectProfileRepository>();
        var currentUser = CreateCurrentUser(userId: 42, customerId: 77);
        repository.GetAvailableProfilesAsync(new UserId(42), new CustomerId(77))
            .Returns((IReadOnlyCollection<ProfileRecord>)[new ProfileRecord(ProfileId: 3, DisplayName: "Specific Profile")]);

        await CreateHandler(repository, currentUser).Handle(new SelectProfileCommand(null), TestContext.Current.CancellationToken);

        await repository.Received(1).GetAvailableProfilesAsync(new UserId(42), new CustomerId(77));
    }
}
