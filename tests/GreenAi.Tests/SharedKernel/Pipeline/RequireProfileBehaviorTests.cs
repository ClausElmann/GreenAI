using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;
using GreenAi.Api.SharedKernel.Pipeline;
using GreenAi.Api.SharedKernel.Results;
using MediatR;
using NSubstitute;

namespace GreenAi.Tests.SharedKernel.Pipeline;

/// <summary>
/// Unit tests for RequireProfileBehavior — the pipeline guard that blocks requests
/// marked IRequireProfile when the current user's ProfileId is 0.
///
/// Critical invariants:
///   - ProfileId = 0 + IRequireProfile → Result.Fail("PROFILE_NOT_SELECTED")
///   - ProfileId > 0 + IRequireProfile → passes through to next
///   - ProfileId = 0 + NO IRequireProfile → passes through (behavior is inert)
///   - Handler (next) is NEVER called when the guard fires
/// </summary>
public sealed class RequireProfileBehaviorTests
{
    // -----------------------------------------------------------------------
    // Local test request stubs
    // -----------------------------------------------------------------------

    /// <summary>Business command that requires a resolved profile.</summary>
    private sealed record ProtectedCommand : IRequest<Result<string>>, IRequireProfile;

    /// <summary>Auth command that does NOT require a resolved profile (e.g. SelectProfile itself).</summary>
    private sealed record UnprotectedCommand : IRequest<Result<string>>;

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static ICurrentUser UserWithProfile(int profileId)
    {
        var user = Substitute.For<ICurrentUser>();
        user.ProfileId.Returns(new ProfileId(profileId));
        return user;
    }

    private static RequireProfileBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(ICurrentUser user)
        where TRequest : IRequest<TResponse>
        => new(user);

    // -----------------------------------------------------------------------
    // RULE: ProfileId = 0 + IRequireProfile → blocked
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Handle_ProfileIdZero_RequestImplementsIRequireProfile_ReturnsFail()
    {
        var user = UserWithProfile(profileId: 0);
        var behavior = CreateBehavior<ProtectedCommand, Result<string>>(user);
        var nextCalled = false;
        Task<Result<string>> Next(CancellationToken ct) { nextCalled = true; return Task.FromResult(Result<string>.Ok("should-not-reach")); }

        var result = await behavior.Handle(new ProtectedCommand(), Next, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("PROFILE_NOT_SELECTED", result.Error!.Code);
        Assert.False(nextCalled, "Handler must not be called when ProfileId = 0.");
    }

    [Fact]
    public async Task Handle_ProfileIdZero_IRequireProfile_ErrorMessageDescribesReason()
    {
        var behavior = CreateBehavior<ProtectedCommand, Result<string>>(UserWithProfile(0));

        var result = await behavior.Handle(new ProtectedCommand(), _ => Task.FromResult(Result<string>.Ok("")), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("profile", result.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_NegativeProfileId_IRequireProfile_ReturnsFail()
    {
        // Defensive: negative ProfileId is also invalid.
        var behavior = CreateBehavior<ProtectedCommand, Result<string>>(UserWithProfile(-1));

        var result = await behavior.Handle(new ProtectedCommand(), _ => Task.FromResult(Result<string>.Ok("")), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("PROFILE_NOT_SELECTED", result.Error!.Code);
    }

    // -----------------------------------------------------------------------
    // RULE: ProfileId > 0 + IRequireProfile → passes through
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Handle_ValidProfileId_RequestImplementsIRequireProfile_CallsNext()
    {
        var user = UserWithProfile(profileId: 7);
        var behavior = CreateBehavior<ProtectedCommand, Result<string>>(user);
        var expected = Result<string>.Ok("handler-result");

        var result = await behavior.Handle(new ProtectedCommand(), _ => Task.FromResult(expected), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("handler-result", result.Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public async Task Handle_AnyPositiveProfileId_IRequireProfile_PassesThrough(int profileId)
    {
        var behavior = CreateBehavior<ProtectedCommand, Result<string>>(UserWithProfile(profileId));

        var result = await behavior.Handle(new ProtectedCommand(), _ => Task.FromResult(Result<string>.Ok("ok")), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // -----------------------------------------------------------------------
    // RULE: No IRequireProfile → behavior is inert regardless of ProfileId
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Handle_ProfileIdZero_RequestDoesNotImplementIRequireProfile_CallsNext()
    {
        // Auth commands (LoginCommand, SelectProfile, etc.) must NOT be blocked
        // even when ProfileId = 0 — they are the mechanism that resolves the profile.
        var behavior = CreateBehavior<UnprotectedCommand, Result<string>>(UserWithProfile(0));
        var nextCalled = false;
        Task<Result<string>> Next(CancellationToken ct) { nextCalled = true; return Task.FromResult(Result<string>.Ok("auth-flow")); }

        var result = await behavior.Handle(new UnprotectedCommand(), Next, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(nextCalled, "Auth commands must not be blocked by RequireProfileBehavior.");
    }
}
