using GreenAi.Api.Features.UserSelfService.UpdateUser;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Ids;
using NSubstitute;

namespace GreenAi.Tests.Features.UserSelfService;

public sealed class UpdateUserHandlerTests
{
    private static ICurrentUser CurrentUser(int userId = 10, int customerId = 20, int profileId = 30)
    {
        var mock = Substitute.For<ICurrentUser>();
        mock.UserId.Returns(new UserId(userId));
        mock.CustomerId.Returns(new CustomerId(customerId));
        mock.ProfileId.Returns(new ProfileId(profileId));
        return mock;
    }

    private static UpdateUserHandler CreateHandler(IDbSession? db = null, ICurrentUser? currentUser = null)
        => new(
            db ?? Substitute.For<IDbSession>(),
            currentUser ?? CurrentUser());

    // ===================================================================
    // DisplayName update
    // ===================================================================

    [Fact]
    public async Task Handle_DisplayNameProvided_ReturnsSuccess()
    {
        var db   = Substitute.For<IDbSession>();
        var handler = CreateHandler(db, CurrentUser(profileId: 30));

        var result = await handler.Handle(
            new UpdateUserCommand("Alice", null),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("Profile updated.", result.Value!.Message);
    }

    [Fact]
    public async Task Handle_DisplayNameProvided_ExecutesDisplayNameSql()
    {
        var db      = Substitute.For<IDbSession>();
        var handler = CreateHandler(db, CurrentUser(profileId: 30));

        await handler.Handle(
            new UpdateUserCommand("Alice", null),
            TestContext.Current.CancellationToken);

        await db.Received(1).ExecuteAsync(
            Arg.Is<string>(s => s.Contains("Profiles")),
            Arg.Any<object>());
    }

    // ===================================================================
    // LanguageId update
    // ===================================================================

    [Fact]
    public async Task Handle_LanguageIdProvided_ReturnsSuccess()
    {
        var db      = Substitute.For<IDbSession>();
        var handler = CreateHandler(db, CurrentUser());

        var result = await handler.Handle(
            new UpdateUserCommand(null, 3),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_LanguageIdProvided_ExecutesLanguageSql()
    {
        var db      = Substitute.For<IDbSession>();
        var handler = CreateHandler(db, CurrentUser(userId: 10, customerId: 20));

        await handler.Handle(
            new UpdateUserCommand(null, 3),
            TestContext.Current.CancellationToken);

        await db.Received(1).ExecuteAsync(
            Arg.Is<string>(s => s.Contains("UserCustomerMemberships")),
            Arg.Any<object>());
    }

    // ===================================================================
    // Both fields provided
    // ===================================================================

    [Fact]
    public async Task Handle_BothFieldsProvided_ExecutesBothSqlStatements()
    {
        var db      = Substitute.For<IDbSession>();
        var handler = CreateHandler(db, CurrentUser());

        await handler.Handle(
            new UpdateUserCommand("Bob", 2),
            TestContext.Current.CancellationToken);

        await db.Received(2).ExecuteAsync(Arg.Any<string>(), Arg.Any<object>());
    }
}
