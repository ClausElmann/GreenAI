using GreenAi.Api.Features.CustomerAdmin.GetUsers;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Tests.Database;
using NSubstitute;

namespace GreenAi.Tests.Features.CustomerAdmin;

/// <summary>
/// Integration tests for GetUsersHandler.
/// Handler injects IDbSession directly — tests use real DB.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class GetUsersHandlerTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly CustomerAdminTestDataBuilder _builder;

    public GetUsersHandlerTests(DatabaseFixture db)
    {
        _db = db;
        _builder = new CustomerAdminTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static GetUsersHandler CreateHandler(CustomerId customerId)
    {
        var db   = new DbSession(DatabaseFixture.ConnectionString);
        var user = Substitute.For<ICurrentUser>();
        user.CustomerId.Returns(customerId);
        user.IsAuthenticated.Returns(true);
        return new GetUsersHandler(db, user);
    }

    [Fact]
    public async Task Handle_CustomerWithActiveUsers_ReturnsUserRows()
    {
        // Arrange
        var customerId = await _builder.InsertCustomerAsync();
        var userId     = await _builder.InsertUserAsync(new() { Email = "active@test.com", IsActive = true });
        await _builder.InsertUserCustomerMembershipAsync(userId, customerId);

        // Act
        var result = await CreateHandler(customerId).Handle(
            new GetUsersQuery(),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!, r => r.Email == "active@test.com");
    }

    [Fact]
    public async Task Handle_NoUsersForCustomer_ReturnsEmptyList()
    {
        // Arrange — customer has no membership rows
        var customerId = await _builder.InsertCustomerAsync("EmptyCustomer");

        // Act
        var result = await CreateHandler(customerId).Handle(
            new GetUsersQuery(),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Handle_OnlyReturnsUsersForCurrentCustomer_TenantIsolation()
    {
        // Arrange — two customers, each with one user
        var customerA = await _builder.InsertCustomerAsync("TenantA");
        var customerB = await _builder.InsertCustomerAsync("TenantB");
        var userA     = await _builder.InsertUserAsync(new() { Email = "a@test.com" });
        var userB     = await _builder.InsertUserAsync(new() { Email = "b@test.com" });
        await _builder.InsertUserCustomerMembershipAsync(userA, customerA);
        await _builder.InsertUserCustomerMembershipAsync(userB, customerB);

        // Act — query as CustomerA
        var result = await CreateHandler(customerA).Handle(
            new GetUsersQuery(),
            TestContext.Current.CancellationToken);

        // Assert — only CustomerA's user returned
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("a@test.com", result.Value![0].Email);
    }
}
