using GreenAi.Api.Features.CustomerAdmin.GetProfiles;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Tests.Database;
using NSubstitute;

namespace GreenAi.Tests.Features.CustomerAdmin;

[Collection(DatabaseCollection.Name)]
public sealed class GetProfileDetailsHandlerTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly CustomerAdminTestDataBuilder _builder;

    public GetProfileDetailsHandlerTests(DatabaseFixture db)
    {
        _db      = db;
        _builder = new CustomerAdminTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static GetProfileDetailsHandler CreateHandler(CustomerId customerId)
    {
        var db   = new DbSession(DatabaseFixture.ConnectionString);
        var user = Substitute.For<ICurrentUser>();
        user.CustomerId.Returns(customerId);
        user.IsAuthenticated.Returns(true);
        return new GetProfileDetailsHandler(db, user);
    }

    [Fact]
    public async Task Handle_ProfileExistsInCustomer_ReturnsProfileRow()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId     = await _builder.InsertUserAsync();
        var profileId  = await _builder.InsertProfileAsync(customerId, userId, "Test Profile");

        var result = await CreateHandler(customerId).Handle(
            new GetProfileDetailsQuery(new ProfileId(profileId)),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Test Profile", result.Value!.Name);
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsProfileNotFound()
    {
        var customerId = await _builder.InsertCustomerAsync();

        var result = await CreateHandler(customerId).Handle(
            new GetProfileDetailsQuery(new ProfileId(999_999)),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("PROFILE_NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ProfileFromOtherCustomer_ReturnsNotFound()
    {
        // Tenant isolation: profile belonging to different customer must not be accessible
        var customerA  = await _builder.InsertCustomerAsync("CustomerA");
        var customerB  = await _builder.InsertCustomerAsync("CustomerB");
        var userId     = await _builder.InsertUserAsync();
        var profileId  = await _builder.InsertProfileAsync(customerA, userId, "CustomerA Profile");

        var result = await CreateHandler(customerB).Handle(
            new GetProfileDetailsQuery(new ProfileId(profileId)),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("PROFILE_NOT_FOUND", result.Error!.Code);
    }
}
