using GreenAi.Api.Features.CustomerAdmin.GetProfiles;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Tests.Database;
using NSubstitute;

namespace GreenAi.Tests.Features.CustomerAdmin;

/// <summary>
/// Integration tests for GetProfilesHandler and GetProfileDetailsHandler.
/// Handler injects IDbSession directly (no repository) — tests use real DB.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class GetProfilesHandlerTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly CustomerAdminTestDataBuilder _builder;

    public GetProfilesHandlerTests(DatabaseFixture db)
    {
        _db = db;
        _builder = new CustomerAdminTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static (GetProfilesHandler handler, GetProfileDetailsHandler detailsHandler) CreateHandlers(CustomerId customerId)
    {
        var db = new DbSession(DatabaseFixture.ConnectionString);
        var user = Substitute.For<ICurrentUser>();
        user.CustomerId.Returns(customerId);
        user.IsAuthenticated.Returns(true);
        return (new GetProfilesHandler(db, user), new GetProfileDetailsHandler(db, user));
    }

    // ===================================================================
    // GetProfilesHandler
    // ===================================================================

    [Fact]
    public async Task Handle_CustomerWithProfiles_ReturnsAllProfiles()
    {
        // Arrange
        var customerId = await _builder.InsertCustomerAsync("ProfileCustomer");
        var userId     = await _builder.InsertUserAsync();
        await _builder.InsertProfileAsync(customerId, userId, "Profile A");
        await _builder.InsertProfileAsync(customerId, userId, "Profile B");

        var (handler, _) = CreateHandlers(customerId);

        // Act
        var result = await handler.Handle(new GetProfilesQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task Handle_CustomerWithNoProfiles_ReturnsEmptyList()
    {
        // Arrange — customer exists but has no profiles
        var customerId = await _builder.InsertCustomerAsync("EmptyCustomer");
        var (handler, _) = CreateHandlers(customerId);

        // Act
        var result = await handler.Handle(new GetProfilesQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Handle_OnlyReturnsProfilesForCurrentCustomer_NotOthers()
    {
        // Arrange — two customers, each with one profile
        var customerA  = await _builder.InsertCustomerAsync("CustomerA");
        var customerB  = await _builder.InsertCustomerAsync("CustomerB");
        var userA      = await _builder.InsertUserAsync(new() { Email = "a@test.com" });
        var userB      = await _builder.InsertUserAsync(new() { Email = "b@test.com" });
        await _builder.InsertProfileAsync(customerA, userA, "ProfileA");
        await _builder.InsertProfileAsync(customerB, userB, "ProfileB");

        // Act — query as CustomerA
        var (handlerA, _) = CreateHandlers(customerA);
        var result = await handlerA.Handle(new GetProfilesQuery(), TestContext.Current.CancellationToken);

        // Assert — only CustomerA's profile returned (tenant isolation)
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("ProfileA", result.Value![0].Name);
    }

    // ===================================================================
    // GetProfileDetailsHandler
    // ===================================================================

    [Fact]
    public async Task HandleDetails_ExistingProfile_ReturnsProfileRow()
    {
        // Arrange
        var customerId = await _builder.InsertCustomerAsync();
        var userId     = await _builder.InsertUserAsync();
        var profileId  = await _builder.InsertProfileAsync(customerId, userId, "Detail Profile");

        var (_, detailsHandler) = CreateHandlers(customerId);

        // Act
        var result = await detailsHandler.Handle(
            new GetProfileDetailsQuery(new ProfileId(profileId)),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Detail Profile", result.Value!.Name);
    }

    [Fact]
    public async Task HandleDetails_UnknownProfileId_ReturnsNotFound()
    {
        // Arrange
        var customerId = await _builder.InsertCustomerAsync();
        var (_, detailsHandler) = CreateHandlers(customerId);

        // Act
        var result = await detailsHandler.Handle(
            new GetProfileDetailsQuery(new ProfileId(99999)),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("PROFILE_NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task HandleDetails_ProfileBelongingToOtherCustomer_ReturnsNotFound()
    {
        // Arrange — tenant isolation: profile exists for CustomerB, request as CustomerA
        var customerA = await _builder.InsertCustomerAsync("TenantA");
        var customerB = await _builder.InsertCustomerAsync("TenantB");
        var userB     = await _builder.InsertUserAsync(new() { Email = "b@test.com" });
        var profileId = await _builder.InsertProfileAsync(customerB, userB, "B's Profile");

        var (_, detailsHandlerAsA) = CreateHandlers(customerA);

        // Act — attempt to read CustomerB's profile as CustomerA
        var result = await detailsHandlerAsA.Handle(
            new GetProfileDetailsQuery(new ProfileId(profileId)),
            TestContext.Current.CancellationToken);

        // Assert — SQL has WHERE CustomerId = @CustomerId → returns null → NOT_FOUND
        Assert.False(result.IsSuccess);
        Assert.Equal("PROFILE_NOT_FOUND", result.Error!.Code);
    }
}
