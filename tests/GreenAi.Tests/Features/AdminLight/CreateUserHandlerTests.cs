using GreenAi.Api.Features.AdminLight.CreateUser;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Permissions;
using NSubstitute;

namespace GreenAi.Tests.Features.AdminLight;

public sealed class CreateUserHandlerTests
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
        mock.DoesUserHaveRoleAsync(Arg.Any<UserId>(), UserRoleNames.ManageUsers).Returns(canManage);
        mock.IsUserSuperAdminAsync(Arg.Any<UserId>()).Returns(isSuperAdmin);
        return mock;
    }

    private static CreateUserHandler CreateHandler(
        ICreateUserRepository? repository   = null,
        ICurrentUser?          currentUser  = null,
        IPermissionService?    permissions  = null)
        => new(
            repository  ?? Substitute.For<ICreateUserRepository>(),
            currentUser ?? CurrentUser(),
            permissions ?? PermissionsFor(canManage: true));

    // ===================================================================
    // Permission gate
    // ===================================================================

    [Fact]
    public async Task Handle_CallerLacksPermission_ReturnsForbidden()
    {
        var permissions = PermissionsFor(canManage: false, isSuperAdmin: false);

        var result = await CreateHandler(permissions: permissions).Handle(
            new CreateUserCommand("new@test.local", "pass123"),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_SuperAdminBypass_DoesNotReturnForbidden()
    {
        var repo        = Substitute.For<ICreateUserRepository>();
        var permissions = PermissionsFor(canManage: false, isSuperAdmin: true);
        repo.EmailExistsAsync(Arg.Any<string>()).Returns(false);
        repo.InsertUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new UserId(42));

        var result = await CreateHandler(repo, permissions: permissions).Handle(
            new CreateUserCommand("new@test.local", "pass123"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    // ===================================================================
    // Email taken
    // ===================================================================

    [Fact]
    public async Task Handle_EmailAlreadyExists_ReturnsEmailTaken()
    {
        var repo = Substitute.For<ICreateUserRepository>();
        repo.EmailExistsAsync("taken@test.local").Returns(true);

        var result = await CreateHandler(repo).Handle(
            new CreateUserCommand("taken@test.local", "pass123"),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("EMAIL_TAKEN", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_DoesNotInsertUser()
    {
        var repo = Substitute.For<ICreateUserRepository>();
        repo.EmailExistsAsync(Arg.Any<string>()).Returns(true);

        await CreateHandler(repo).Handle(
            new CreateUserCommand("taken@test.local", "pass123"),
            TestContext.Current.CancellationToken);

        await repo.DidNotReceive().InsertUserAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // ===================================================================
    // Happy path
    // ===================================================================

    [Fact]
    public async Task Handle_ValidInput_ReturnsSuccess()
    {
        var repo = Substitute.For<ICreateUserRepository>();
        repo.EmailExistsAsync(Arg.Any<string>()).Returns(false);
        repo.InsertUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new UserId(42));

        var result = await CreateHandler(repo).Handle(
            new CreateUserCommand("newuser@test.local", "pass123"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(new UserId(42), result.Value!.UserId);
    }

    [Fact]
    public async Task Handle_ValidInput_InsertsUserWithHashedPassword()
    {
        var repo = Substitute.For<ICreateUserRepository>();
        repo.EmailExistsAsync(Arg.Any<string>()).Returns(false);
        repo.InsertUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new UserId(42));

        string? capturedHash = null;
        await repo.InsertUserAsync(
            Arg.Any<string>(),
            Arg.Do<string>(h => capturedHash = h),
            Arg.Any<string>());

        await CreateHandler(repo).Handle(
            new CreateUserCommand("newuser@test.local", "plaintext123"),
            TestContext.Current.CancellationToken);

        Assert.NotNull(capturedHash);
        Assert.NotEqual("plaintext123", capturedHash); // must be hashed
    }

    [Fact]
    public async Task Handle_ValidInput_InsertsMembershipForCallerCustomer()
    {
        var repo        = Substitute.For<ICreateUserRepository>();
        var currentUser = CurrentUser(userId: 1, customerId: 10);
        repo.EmailExistsAsync(Arg.Any<string>()).Returns(false);
        repo.InsertUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new UserId(42));

        await CreateHandler(repo, currentUser).Handle(
            new CreateUserCommand("newuser@test.local", "pass123"),
            TestContext.Current.CancellationToken);

        await repo.Received(1).InsertMembershipAsync(
            new UserId(42),
            new CustomerId(10),
            Arg.Any<int>());
    }
}
