using GreenAi.Api.Features.Auth.SelectCustomer;
using GreenAi.Api.Features.Auth.SelectProfile;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GreenAi.Tests.Features.Auth;

public sealed class SelectCustomerHandlerTests
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

    private static ICurrentUser CreateCurrentUser(int userId = 1, string email = "user@example.com")
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(new UserId(userId));
        user.Email.Returns(email);
        return user;
    }

    private static SelectCustomerHandler CreateHandler(
        ISelectCustomerRepository repository,
        ICurrentUser? currentUser = null,
        IRefreshTokenWriter? tokenWriter = null)
        => new(repository, CreateJwtService(), currentUser ?? CreateCurrentUser(), tokenWriter ?? Substitute.For<IRefreshTokenWriter>());

    /// <summary>Returns a single-profile collection — the standard auto-resolve case.</summary>
    private static IReadOnlyCollection<ProfileRecord> OneProfile(int profileId = 9, string displayName = "Main Profile")
        => [new ProfileRecord(profileId, displayName)];

    // ===================================================================
    // Happy path — single profile auto-resolve
    // ===================================================================

    [Fact]
    public async Task Handle_ValidMembershipSingleProfile_ReturnsToken()
    {
        var repository = Substitute.For<ISelectCustomerRepository>();
        repository.FindMembershipAsync(new UserId(1), new CustomerId(10))
            .Returns(new MembershipRecord(CustomerId: 10, LanguageId: 2));
        repository.GetProfilesAsync(new UserId(1), new CustomerId(10))
            .Returns((IReadOnlyCollection<ProfileRecord>)OneProfile());

        var result = await CreateHandler(repository).Handle(new SelectCustomerCommand(10), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.AccessToken);
        Assert.NotEmpty(result.Value.RefreshToken);
    }

    [Fact]
    public async Task Handle_ValidMembershipSingleProfile_SavesRefreshTokenWithRealProfileId()
    {
        // RULE: ProfileId must be > 0 in the saved token. ProfileId(0) is forbidden from Step 11+.
        var repository = Substitute.For<ISelectCustomerRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        var currentUser = CreateCurrentUser(userId: 1, email: "user@example.com");
        repository.FindMembershipAsync(new UserId(1), new CustomerId(10))
            .Returns(new MembershipRecord(CustomerId: 10, LanguageId: 2));
        repository.GetProfilesAsync(new UserId(1), new CustomerId(10))
            .Returns((IReadOnlyCollection<ProfileRecord>)OneProfile(profileId: 9));

        await CreateHandler(repository, currentUser, tokenWriter).Handle(new SelectCustomerCommand(10), TestContext.Current.CancellationToken);

        await tokenWriter.Received(1).SaveAsync(
            new UserId(1),
            new CustomerId(10),
            new ProfileId(9),       // must be the real Profiles.Id — never 0
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            2);                     // languageId from membership
    }

    [Fact]
    public async Task Handle_ValidMembership_UsesUserIdFromCurrentUser()
    {
        // RULE: UserId must originate from ICurrentUser (JWT claim), never from client-supplied data.
        var repository = Substitute.For<ISelectCustomerRepository>();
        var currentUser = CreateCurrentUser(userId: 42, email: "bob@example.com");
        repository.FindMembershipAsync(new UserId(42), new CustomerId(10))
            .Returns(new MembershipRecord(CustomerId: 10, LanguageId: 1));
        repository.GetProfilesAsync(new UserId(42), new CustomerId(10))
            .Returns((IReadOnlyCollection<ProfileRecord>)OneProfile());

        await CreateHandler(repository, currentUser).Handle(new SelectCustomerCommand(10), TestContext.Current.CancellationToken);

        await repository.Received(1).FindMembershipAsync(new UserId(42), new CustomerId(10));
    }

    // ===================================================================
    // Multiple profiles — NeedsProfileSelection
    // ===================================================================

    [Fact]
    public async Task Handle_ValidMembershipMultipleProfiles_ReturnsRequiresProfileSelection()
    {
        var repository = Substitute.For<ISelectCustomerRepository>();
        repository.FindMembershipAsync(new UserId(1), new CustomerId(10))
            .Returns(new MembershipRecord(CustomerId: 10, LanguageId: 1));
        repository.GetProfilesAsync(new UserId(1), new CustomerId(10))
            .Returns((IReadOnlyCollection<ProfileRecord>)[
                new ProfileRecord(1, "Work"),
                new ProfileRecord(2, "Personal")
            ]);

        var result = await CreateHandler(repository).Handle(new SelectCustomerCommand(10), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.NeedsProfileSelection);
        Assert.Equal(2, result.Value.AvailableProfiles!.Count);
    }

    // ===================================================================
    // Error paths
    // ===================================================================

    [Fact]
    public async Task Handle_NoProfiles_ReturnsError()
    {
        var repository = Substitute.For<ISelectCustomerRepository>();
        repository.FindMembershipAsync(new UserId(1), new CustomerId(10))
            .Returns(new MembershipRecord(CustomerId: 10, LanguageId: 1));
        repository.GetProfilesAsync(new UserId(1), new CustomerId(10))
            .Returns((IReadOnlyCollection<ProfileRecord>)[]);

        var result = await CreateHandler(repository).Handle(new SelectCustomerCommand(10), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("PROFILE_NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_MembershipNotFound_ReturnsError()
    {
        var repository = Substitute.For<ISelectCustomerRepository>();
        repository.FindMembershipAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>())
            .Returns((MembershipRecord?)null);

        var result = await CreateHandler(repository).Handle(new SelectCustomerCommand(99), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("MEMBERSHIP_NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_InactiveMembership_ReturnsError()
    {
        // Inactive memberships are filtered by SQL (WHERE IsActive = 1) — the repository
        // returns null, which the handler treats identically to not-found.
        var repository = Substitute.For<ISelectCustomerRepository>();
        repository.FindMembershipAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>())
            .Returns((MembershipRecord?)null);

        var result = await CreateHandler(repository).Handle(new SelectCustomerCommand(10), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("MEMBERSHIP_NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_MembershipNotFound_DoesNotSaveRefreshToken()
    {
        var repository = Substitute.For<ISelectCustomerRepository>();
        var tokenWriter = Substitute.For<IRefreshTokenWriter>();
        repository.FindMembershipAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>())
            .Returns((MembershipRecord?)null);

        await CreateHandler(repository, tokenWriter: tokenWriter).Handle(new SelectCustomerCommand(99), TestContext.Current.CancellationToken);

        await tokenWriter.DidNotReceive().SaveAsync(
            Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<ProfileId>(),
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>());
    }
}
