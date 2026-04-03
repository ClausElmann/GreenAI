using GreenAi.Api.Features.Localization.BatchUpsertLabels;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Permissions;
using NSubstitute;

namespace GreenAi.Tests.Features.Localization.BatchUpsertLabels;

public sealed class BatchUpsertLabelsHandlerTests
{
    private static ICurrentUser CreateUser(int userId = 1)
    {
        var user = Substitute.For<ICurrentUser>();
        user.UserId.Returns(new UserId(userId));
        return user;
    }

    private static BatchUpsertLabelsHandler CreateHandler(
        IBatchUpsertLabelsRepository? repository = null,
        ICurrentUser? user = null,
        IPermissionService? permissions = null)
        => new(
            repository ?? Substitute.For<IBatchUpsertLabelsRepository>(),
            user ?? CreateUser(),
            permissions ?? Substitute.For<IPermissionService>());

    // ===================================================================
    // Authorization
    // ===================================================================

    [Fact]
    public async Task Handle_NotSuperAdmin_ReturnsForbidden()
    {
        var permissions = Substitute.For<IPermissionService>();
        permissions.IsUserSuperAdminAsync(Arg.Any<UserId>()).Returns(false);

        var result = await CreateHandler(permissions: permissions)
            .Handle(new BatchUpsertLabelsCommand([]), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.Error?.Code);
    }

    [Fact]
    public async Task Handle_IsSuperAdmin_DoesNotReturnForbidden()
    {
        var permissions = Substitute.For<IPermissionService>();
        permissions.IsUserSuperAdminAsync(Arg.Any<UserId>()).Returns(true);

        var result = await CreateHandler(permissions: permissions)
            .Handle(new BatchUpsertLabelsCommand([]), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    // ===================================================================
    // Empty labels
    // ===================================================================

    [Fact]
    public async Task Handle_EmptyLabels_ReturnsZeroCount()
    {
        var permissions = Substitute.For<IPermissionService>();
        permissions.IsUserSuperAdminAsync(Arg.Any<UserId>()).Returns(true);

        var result = await CreateHandler(permissions: permissions)
            .Handle(new BatchUpsertLabelsCommand([]), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.UpsertedCount);
    }

    [Fact]
    public async Task Handle_EmptyLabels_DoesNotCallRepository()
    {
        var repository = Substitute.For<IBatchUpsertLabelsRepository>();
        var permissions = Substitute.For<IPermissionService>();
        permissions.IsUserSuperAdminAsync(Arg.Any<UserId>()).Returns(true);

        await CreateHandler(repository, permissions: permissions)
            .Handle(new BatchUpsertLabelsCommand([]), TestContext.Current.CancellationToken);

        await repository.DidNotReceive().UpsertAsync(Arg.Any<LabelEntry>());
    }

    // ===================================================================
    // Happy path
    // ===================================================================

    [Fact]
    public async Task Handle_TwoLabels_CallsRepositoryTwice()
    {
        var repository = Substitute.For<IBatchUpsertLabelsRepository>();
        var permissions = Substitute.For<IPermissionService>();
        permissions.IsUserSuperAdminAsync(Arg.Any<UserId>()).Returns(true);

        var labels = new List<LabelEntry>
        {
            new("shared.save", "Save", 1),
            new("shared.cancel", "Cancel", 1)
        };

        await CreateHandler(repository, permissions: permissions)
            .Handle(new BatchUpsertLabelsCommand(labels), TestContext.Current.CancellationToken);

        await repository.Received(2).UpsertAsync(Arg.Any<LabelEntry>());
    }

    [Fact]
    public async Task Handle_TwoLabels_ReturnsCountTwo()
    {
        var repository = Substitute.For<IBatchUpsertLabelsRepository>();
        var permissions = Substitute.For<IPermissionService>();
        permissions.IsUserSuperAdminAsync(Arg.Any<UserId>()).Returns(true);

        var labels = new List<LabelEntry>
        {
            new("shared.save", "Save", 1),
            new("shared.cancel", "Cancel", 1)
        };

        var result = await CreateHandler(repository, permissions: permissions)
            .Handle(new BatchUpsertLabelsCommand(labels), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.UpsertedCount);
    }

    [Fact]
    public async Task Handle_TwoLabels_CallsRepositoryWithEachLabel()
    {
        var repository = Substitute.For<IBatchUpsertLabelsRepository>();
        var permissions = Substitute.For<IPermissionService>();
        permissions.IsUserSuperAdminAsync(Arg.Any<UserId>()).Returns(true);

        var label1 = new LabelEntry("shared.save", "Save", 1);
        var label2 = new LabelEntry("shared.cancel", "Cancel", 1);

        await CreateHandler(repository, permissions: permissions)
            .Handle(new BatchUpsertLabelsCommand([label1, label2]), TestContext.Current.CancellationToken);

        await repository.Received(1).UpsertAsync(label1);
        await repository.Received(1).UpsertAsync(label2);
    }
}
