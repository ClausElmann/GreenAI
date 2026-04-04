using GreenAi.Api.Features.AdminLight.AssignRole;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Permissions;
using NSubstitute;

namespace GreenAi.Tests.Features.AdminLight;

public sealed class AssignRoleHandlerTests
{
    private static ICurrentUser CurrentUser(int userId = 1)
    {
        var mock = Substitute.For<ICurrentUser>();
        mock.UserId.Returns(new UserId(userId));
        mock.CustomerId.Returns(new CustomerId(10));
        mock.ProfileId.Returns(new ProfileId(99));
        return mock;
    }

    private static IPermissionService PermissionsFor(bool canManage, bool isSuperAdmin = false)
    {
        var mock = Substitute.For<IPermissionService>();
        mock.DoesUserHaveRoleAsync(Arg.Any<UserId>(), UserRoleNames.ManageUsers).Returns(canManage);
        mock.IsUserSuperAdminAsync(Arg.Any<UserId>()).Returns(isSuperAdmin);
        return mock;
    }

    private static AssignRoleHandler CreateHandler(
        IAssignRoleRepository? repository  = null,
        ICurrentUser?          currentUser = null,
        IPermissionService?    permissions = null)
        => new(
            repository  ?? Substitute.For<IAssignRoleRepository>(),
            currentUser ?? CurrentUser(),
            permissions ?? PermissionsFor(canManage: true));

    // ===================================================================
    // Permission
    // ===================================================================

    [Fact]
    public async Task Handle_CallerLacksPermission_ReturnsForbidden()
    {
        var result = await CreateHandler(permissions: PermissionsFor(canManage: false)).Handle(
            new AssignRoleCommand(5, UserRoleNames.ManageUsers),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    // ===================================================================
    // Role not found
    // ===================================================================

    [Fact]
    public async Task Handle_RoleNotFound_ReturnsNotFound()
    {
        var repo = Substitute.For<IAssignRoleRepository>();
        repo.RoleExistsAsync(Arg.Any<string>()).Returns(false);

        var result = await CreateHandler(repo).Handle(
            new AssignRoleCommand(5, "NonExistent"),
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
        var repo = Substitute.For<IAssignRoleRepository>();
        repo.RoleExistsAsync(UserRoleNames.ManageUsers).Returns(true);

        var result = await CreateHandler(repo).Handle(
            new AssignRoleCommand(5, UserRoleNames.ManageUsers),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ValidInput_CallsAssignRole()
    {
        var repo = Substitute.For<IAssignRoleRepository>();
        repo.RoleExistsAsync(UserRoleNames.ManageUsers).Returns(true);

        await CreateHandler(repo).Handle(
            new AssignRoleCommand(5, UserRoleNames.ManageUsers),
            TestContext.Current.CancellationToken);

        await repo.Received(1).AssignRoleAsync(new UserId(5), UserRoleNames.ManageUsers);
    }
}
