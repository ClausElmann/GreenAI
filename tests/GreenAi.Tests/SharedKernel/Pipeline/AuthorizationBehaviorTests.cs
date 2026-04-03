using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;
using NSubstitute;

namespace GreenAi.Tests.SharedKernel.Pipeline;

/// <summary>
/// Unit tests for AuthorizationBehavior — the pipeline guard that blocks unauthenticated
/// requests marked IRequireAuthentication.
///
/// Critical invariants:
///   - IsAuthenticated=false + IRequireAuthentication → Result.Fail("UNAUTHORIZED")
///   - IsAuthenticated=true  + IRequireAuthentication → passes through to handler
///   - IsAuthenticated=false + no marker             → passes through (behavior is inert)
///   - Handler (next) is NEVER called when the guard fires
/// </summary>
public sealed class AuthorizationBehaviorTests
{
    private sealed record ProtectedCommand : IRequest<Result<string>>, IRequireAuthentication;
    private sealed record UnprotectedCommand : IRequest<Result<string>>;

    private static ICurrentUser UserWith(bool isAuthenticated)
    {
        var user = Substitute.For<ICurrentUser>();
        user.IsAuthenticated.Returns(isAuthenticated);
        return user;
    }

    private static AuthorizationBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(ICurrentUser user)
        where TRequest : IRequest<TResponse>
        => new(user);

    // ===================================================================
    // Blocked cases — IRequireAuthentication + unauthenticated
    // ===================================================================

    [Fact]
    public async Task Handle_Unauthenticated_WithMarker_ReturnsUnauthorized()
    {
        var user     = UserWith(isAuthenticated: false);
        var behavior = CreateBehavior<ProtectedCommand, Result<string>>(user);
        var nextCalled = false;
        Task<Result<string>> Next(CancellationToken ct) { nextCalled = true; return Task.FromResult(Result<string>.Ok("should-not-reach")); }

        var result = await behavior.Handle(new ProtectedCommand(), Next, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_Unauthenticated_WithMarker_DoesNotCallNext()
    {
        var user     = UserWith(isAuthenticated: false);
        var behavior = CreateBehavior<ProtectedCommand, Result<string>>(user);
        var nextCallCount = 0;
        Task<Result<string>> Next(CancellationToken ct) { nextCallCount++; return Task.FromResult(Result<string>.Ok("x")); }

        await behavior.Handle(new ProtectedCommand(), Next, TestContext.Current.CancellationToken);

        Assert.Equal(0, nextCallCount);
    }

    // ===================================================================
    // Pass-through cases — authenticated or no marker
    // ===================================================================

    [Fact]
    public async Task Handle_Authenticated_WithMarker_CallsNext()
    {
        var user     = UserWith(isAuthenticated: true);
        var behavior = CreateBehavior<ProtectedCommand, Result<string>>(user);
        var nextCalled = false;
        Task<Result<string>> Next(CancellationToken ct) { nextCalled = true; return Task.FromResult(Result<string>.Ok("ok")); }

        var result = await behavior.Handle(new ProtectedCommand(), Next, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Handle_Unauthenticated_WithoutMarker_CallsNext()
    {
        // No IRequireAuthentication marker → behavior is a pass-through regardless of auth state
        var user     = UserWith(isAuthenticated: false);
        var behavior = CreateBehavior<UnprotectedCommand, Result<string>>(user);
        var nextCalled = false;
        Task<Result<string>> Next(CancellationToken ct) { nextCalled = true; return Task.FromResult(Result<string>.Ok("ok")); }

        var result = await behavior.Handle(new UnprotectedCommand(), Next, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(nextCalled);
    }
}
