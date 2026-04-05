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

    private static SelectCustomerHandler CreateHandler(ISelectCustomerRepository repository, ICurrentUser? currentUser = null)
        => new(repository, CreateJwtService(), currentUser ?? CreateCurrentUser());

    // ===================================================================
    // Happy path
    // ===================================================================

    private static IReadOnlyCollection<ProfileRecord> SingleProfile(int profileId = 1) =>
        [new ProfileRecord(profileId, "Auto Profile")];

    [Fact]
    public async Task Handle_ValidMembership_ReturnsToken()
    {
        var repository = Substitute.For<ISelectCustomerRepository>();
        repository.FindMembershipAsync(new UserId(1), new CustomerId(10))
            .Returns(new MembershipRecord(CustomerId: 10, LanguageId: 2, DefaultProfileId: 0));
        repository.GetProfilesAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>())
            .Returns(SingleProfile());
        repository.SaveRefreshTokenAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>())
            .Returns(Task.CompletedTask);

        var result = await CreateHandler(repository).Handle(new SelectCustomerCommand(10), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.AccessToken);
        Assert.NotEmpty(result.Value.RefreshToken);
    }

    [Fact]
    public async Task Handle_ValidMembership_SavesRefreshTokenWithMembershipLanguageId()
    {
        // RULE: LanguageId in the saved token must come from the membership row,
        //       never be hardcoded or defaulted by the handler.
        var repository = Substitute.For<ISelectCustomerRepository>();
        var currentUser = CreateCurrentUser(userId: 1, email: "user@example.com");
        repository.FindMembershipAsync(new UserId(1), new CustomerId(10))
            .Returns(new MembershipRecord(CustomerId: 10, LanguageId: 2, DefaultProfileId: 0));
        repository.GetProfilesAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>())
            .Returns(SingleProfile());
        repository.SaveRefreshTokenAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>())
            .Returns(Task.CompletedTask);

        await CreateHandler(repository, currentUser).Handle(new SelectCustomerCommand(10), TestContext.Current.CancellationToken);

        await repository.Received(1).SaveRefreshTokenAsync(
            new UserId(1),
            new CustomerId(10),
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            2); // languageId must equal membership.LanguageId
    }

    [Fact]
    public async Task Handle_ValidMembership_UsesUserIdFromCurrentUser()
    {
        // RULE: UserId must originate from ICurrentUser (JWT claim), never from client-supplied data.
        var repository = Substitute.For<ISelectCustomerRepository>();
        var currentUser = CreateCurrentUser(userId: 42, email: "bob@example.com");
        repository.FindMembershipAsync(new UserId(42), new CustomerId(10))
            .Returns(new MembershipRecord(CustomerId: 10, LanguageId: 1, DefaultProfileId: 0));
        repository.GetProfilesAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>())
            .Returns(SingleProfile());
        repository.SaveRefreshTokenAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>())
            .Returns(Task.CompletedTask);

        await CreateHandler(repository, currentUser).Handle(new SelectCustomerCommand(10), TestContext.Current.CancellationToken);

        await repository.Received(1).FindMembershipAsync(new UserId(42), new CustomerId(10));
    }

    // ===================================================================
    // Error paths
    // ===================================================================

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
        repository.FindMembershipAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>())
            .Returns((MembershipRecord?)null);

        await CreateHandler(repository).Handle(new SelectCustomerCommand(99), TestContext.Current.CancellationToken);

        await repository.DidNotReceive().SaveRefreshTokenAsync(
            Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>());
    }

    [Fact]
    public async Task Handle_ValidMembershipMultipleProfiles_ReturnsRequiresProfileSelectionWithPreAuthToken()
    {
        var repository = Substitute.For<ISelectCustomerRepository>();
        repository.FindMembershipAsync(new UserId(1), new CustomerId(10))
            .Returns(new MembershipRecord(CustomerId: 10, LanguageId: 1, DefaultProfileId: 0));
        repository.GetProfilesAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>())
            .Returns((IReadOnlyCollection<ProfileRecord>)[
                new ProfileRecord(ProfileId: 1, DisplayName: "Alpha"),
                new ProfileRecord(ProfileId: 2, DisplayName: "Beta")]);

        var result = await CreateHandler(repository).Handle(new SelectCustomerCommand(10), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.NeedsProfileSelection);
        Assert.Equal(2, result.Value.AvailableProfiles!.Count);
        Assert.NotNull(result.Value.PreAuthToken);
        Assert.NotEmpty(result.Value.PreAuthToken);
        await repository.DidNotReceive().SaveRefreshTokenAsync(
            Arg.Any<UserId>(), Arg.Any<CustomerId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>());
    }
}
