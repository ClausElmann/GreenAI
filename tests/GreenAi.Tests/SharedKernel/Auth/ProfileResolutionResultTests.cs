using GreenAi.Api.Features.Auth.SelectProfile;
using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Tests.SharedKernel.Auth;

public sealed class ProfileResolutionResultTests
{
    private static ProfileRecord P(int id, string name = "Profile") => new(id, name);

    // ── Zero profiles ────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_EmptyList_IsNotFound()
    {
        var result = ProfileResolutionResult.Resolve([]);
        Assert.True(result.IsNotFound);
        Assert.False(result.IsResolved);
        Assert.False(result.NeedsSelection);
    }

    // ── Single profile ───────────────────────────────────────────────────────

    [Fact]
    public void Resolve_SingleProfile_IsResolved()
    {
        var result = ProfileResolutionResult.Resolve([P(42)]);
        Assert.True(result.IsResolved);
        Assert.False(result.IsNotFound);
        Assert.False(result.NeedsSelection);
    }

    [Fact]
    public void Resolve_SingleProfile_ResolvedIdMatchesRecord()
    {
        var result = ProfileResolutionResult.Resolve([P(42)]);
        Assert.Equal(new ProfileId(42), result.ResolvedId);
    }

    [Fact]
    public void Resolve_SingleProfile_ResolvedIdIsNeverDefault()
    {
        // ProfileId(0) is forbidden from being issued as a resolved identity.
        // Any real profile id must be > 0 by schema; Resolve must propagate it intact.
        var result = ProfileResolutionResult.Resolve([P(7)]);
        Assert.NotEqual(default, result.ResolvedId);
    }

    // ── Multiple profiles ────────────────────────────────────────────────────

    [Fact]
    public void Resolve_MultipleProfiles_NeedsSelection()
    {
        var result = ProfileResolutionResult.Resolve([P(1), P(2)]);
        Assert.True(result.NeedsSelection);
        Assert.False(result.IsNotFound);
        Assert.False(result.IsResolved);
    }

    [Fact]
    public void Resolve_MultipleProfiles_SummariesContainAllProfiles()
    {
        var result = ProfileResolutionResult.Resolve([P(1), P(2), P(3)]);
        Assert.Equal(3, result.Summaries.Count);
    }

    [Fact]
    public void Resolve_MultipleProfiles_SummariesHaveCorrectIds()
    {
        var result = ProfileResolutionResult.Resolve([P(10, "Alpha"), P(20, "Beta")]);
        Assert.Contains(result.Summaries, s => s.ProfileId == 10 && s.DisplayName == "Alpha");
        Assert.Contains(result.Summaries, s => s.ProfileId == 20 && s.DisplayName == "Beta");
    }
}
