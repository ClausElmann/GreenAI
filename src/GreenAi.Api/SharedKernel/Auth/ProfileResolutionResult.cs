using GreenAi.Api.Features.Auth.SelectProfile;
using GreenAi.Api.SharedKernel.Ids;

namespace GreenAi.Api.SharedKernel.Auth;

/// <summary>
/// Outcome of profile auto-resolution after customer selection.
///
/// Used by LoginHandler and SelectCustomerHandler to apply the same
/// 0 / 1 / many branching logic without duplicating it.
///
/// SelectProfileHandler has its own logic (explicit selection + access validation)
/// and does NOT use this type.
/// </summary>
public sealed class ProfileResolutionResult
{
    private ProfileResolutionResult() { }

    public bool IsNotFound     { get; private init; }
    public bool NeedsSelection { get; private init; }
    public bool IsResolved     { get; private init; }

    /// <summary>Resolved ProfileId — only valid when IsResolved is true.</summary>
    public ProfileId ResolvedId { get; private init; } = default;

    /// <summary>Profile summaries for the selection UI — only valid when NeedsSelection is true.</summary>
    public IReadOnlyList<ProfileSummary> Summaries { get; private init; } = [];

    /// <summary>
    /// Maps a profile list to a resolution outcome:
    ///   - 0 profiles → NotFound
    ///   - 1 profile  → Resolved (auto-select)
    ///   - N profiles → NeedsSelection (client must choose)
    /// </summary>
    public static ProfileResolutionResult Resolve(IReadOnlyCollection<ProfileRecord> profiles)
    {
        if (profiles.Count == 0)
            return new ProfileResolutionResult { IsNotFound = true };

        if (profiles.Count == 1)
        {
            var single = profiles.First();
            return new ProfileResolutionResult
            {
                IsResolved = true,
                ResolvedId = new ProfileId(single.ProfileId),
            };
        }

        var summaries = profiles
            .Select(p => new ProfileSummary(p.ProfileId, p.DisplayName))
            .ToList();

        return new ProfileResolutionResult
        {
            NeedsSelection = true,
            Summaries = summaries,
        };
    }
}
