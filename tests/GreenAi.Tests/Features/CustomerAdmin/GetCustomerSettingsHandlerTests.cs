using GreenAi.Api.Features.CustomerAdmin.GetCustomerSettings;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Tests.Database;
using NSubstitute;

namespace GreenAi.Tests.Features.CustomerAdmin;

[Collection(DatabaseCollection.Name)]
public sealed class GetCustomerSettingsHandlerTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private readonly CustomerAdminTestDataBuilder _builder;

    public GetCustomerSettingsHandlerTests(DatabaseFixture db)
    {
        _db      = db;
        _builder = new CustomerAdminTestDataBuilder(DatabaseFixture.ConnectionString);
    }

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static GetCustomerSettingsHandler CreateHandler(CustomerId customerId)
    {
        var db   = new DbSession(DatabaseFixture.ConnectionString);
        var user = Substitute.For<ICurrentUser>();
        user.CustomerId.Returns(customerId);
        user.IsAuthenticated.Returns(true);
        return new GetCustomerSettingsHandler(db, user);
    }

    [Fact]
    public async Task Handle_ExistingCustomer_ReturnsCustomerSettings()
    {
        var customerId = await _builder.InsertCustomerAsync("SettingsTestCustomer");

        var result = await CreateHandler(customerId).Handle(
            new GetCustomerSettingsQuery(),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("SettingsTestCustomer", result.Value!.Name);
    }

    [Fact]
    public async Task Handle_NonExistentCustomer_ReturnsNotFound()
    {
        var result = await CreateHandler(new CustomerId(999_999)).Handle(
            new GetCustomerSettingsQuery(),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }
}
