namespace GreenAi.Api.Features.Auth.GetProfileContext;

/// <summary>
/// Display names for the active profile and customer, shown in TopBar.
/// </summary>
public sealed record GetProfileContextResponse(
    string ProfileDisplayName,
    string CustomerName);
