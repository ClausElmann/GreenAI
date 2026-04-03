using GreenAi.Api.Features.CustomerAdmin.GetUsers;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Tests.Database;
using NSubstitute;

namespace GreenAi.Tests.Features.CustomerAdmin;

[Collection(DatabaseCollection.Name)]
public sealed class GetUserDetailsHandlerTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly CustomerAdminTestDataBuilder _builder;

    public GetUserDetailsHandlerTests(DatabaseFixture db)
    {
        _db      = db;
        _builder = new CustomerAdminTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static GetUserDetailsHandler CreateHandler(CustomerId customerId)
    {
        var db   = new DbSession(DatabaseFixture.ConnectionString);
        var user = Substitute.For<ICurrentUser>();
        user.CustomerId.Returns(customerId);
        user.IsAuthenticated.Returns(true);
        return new GetUserDetailsHandler(db, user);
    }

    [Fact]
    public async Task Handle_UserExistsInCustomer_ReturnsDetails()
    {
        var customerId = await _builder.InsertCustomerAsync();
        var userId     = await _builder.InsertUserAsync(new() { Email = "detail@test.com", IsActive = true });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);

        var result = await CreateHandler(customerId).Handle(
            new GetUserDetailsQuery(userId),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("detail@test.com", result.Value!.Email);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task Handle_UserNotInCustomer_ReturnsNotFound()
    {
        var customerId = await _builder.InsertCustomerAsync();

        var result = await CreateHandler(customerId).Handle(
            new GetUserDetailsQuery(new UserId(999_999)),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }
}
