using GreenAi.Api.Features.AdminLight.AssignProfile;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Permissions;
using NSubstitute;

namespace GreenAi.Tests.Features.AdminLight;

public sealed class AssignProfileHandlerTests
{
    private static ICurrentUser CurrentUser(int userId = 1, int customerId = 10)
    {
        var mock = Substitute.For<ICurrentUser>();
        mock.UserId.Returns(new UserId(userId));
        mock.CustomerId.Returns(new CustomerId(customerId));
        mock.ProfileId.Returns(new ProfileId(99));
        return mock;
    }

    private static IPermissionService PermissionsFor(bool canManage, bool isSuperAdmin = false)
    {
        var mock = Substitute.For<IPermissionService>();
        mock.DoesUserHaveRoleAsync(Arg.Any<UserId>(), UserRoleNames.ManageProfiles).Returns(canManage);
        mock.IsUserSuperAdminAsync(Arg.Any<UserId>()).Returns(isSuperAdmin);
        return mock;
    }

    private static AssignProfileHandler CreateHandler(
        IAssignProfileRepository? repository  = null,
        ICurrentUser?             currentUser = null,
        IPermissionService?       permissions = null)
        => new(
            repository  ?? Substitute.For<IAssignProfileRepository>(),
            currentUser ?? CurrentUser(),
            permissions ?? PermissionsFor(canManage: true));

    // ===================================================================
    // Permission
    // ===================================================================

    [Fact]
    public async Task Handle_CallerLacksPermission_ReturnsForbidden()
    {
        var result = await CreateHandler(permissions: PermissionsFor(canManage: false)).Handle(
            new AssignProfileCommand(5, 20),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    // ===================================================================
    // Tenant isolation
    // ===================================================================

    [Fact]
    public async Task Handle_ProfileNotInCustomer_ReturnsNotFound()
    {
        var repo = Substitute.For<IAssignProfileRepository>();
        repo.ProfileBelongsToCustomerAsync(Arg.Any<int>(), Arg.Any<CustomerId>()).Returns(false);

        var result = await CreateHandler(repo).Handle(
            new AssignProfileCommand(5, 20),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_UserNotInCustomer_ReturnsNotFound()
    {
        var repo = Substitute.For<IAssignProfileRepository>();
        repo.ProfileBelongsToCustomerAsync(Arg.Any<int>(), Arg.Any<CustomerId>()).Returns(true);
        repo.UserBelongsToCustomerAsync(Arg.Any<UserId>(), Arg.Any<CustomerId>()).Returns(false);

        var result = await CreateHandler(repo).Handle(
            new AssignProfileCommand(5, 20),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    // ===================================================================
    // Happy path
    // ===================================================================

    [Fact]
    public async Task Handle_ValidInput_ReturnsSuccess()
    {
        var repo = Substitute.For<IAssignProfileRepository>();
        repo.ProfileBelongsToCustomerAsync(20, Arg.Any<CustomerId>()).Returns(true);
        repo.UserBelongsToCustomerAsync(new UserId(5), Arg.Any<CustomerId>()).Returns(true);

        var result = await CreateHandler(repo).Handle(
            new AssignProfileCommand(5, 20),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ValidInput_CallsAssignProfile()
    {
        var repo = Substitute.For<IAssignProfileRepository>();
        repo.ProfileBelongsToCustomerAsync(20, Arg.Any<CustomerId>()).Returns(true);
        repo.UserBelongsToCustomerAsync(new UserId(5), Arg.Any<CustomerId>()).Returns(true);

        await CreateHandler(repo).Handle(
            new AssignProfileCommand(5, 20),
            TestContext.Current.CancellationToken);

        await repo.Received(1).AssignProfileAsync(new UserId(5), 20);
    }
}
