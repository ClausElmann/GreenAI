namespace GreenAi.Api.SharedKernel.Navigation;

/// <summary>
/// A single navigable destination in the UI shell.
/// Source of truth: docs/SSOT/ui/models/ui-navigation-schema.json (nav_visible_for entries).
/// </summary>
public sealed record NavItem(
    /// <summary>Localization key used to get the display label via ILocalizationContext.Get().</summary>
    string LabelKey,
    /// <summary>Absolute route path (e.g. "/dashboard").</summary>
    string Route,
    /// <summary>MudBlazor icon string constant (Icons.Material.Filled.*).</summary>
    string Icon,
    /// <summary>
    /// UserRoleNames required for this item (any-of match).
    /// Empty array = visible to all authenticated users.
    /// SuperAdmin always satisfies any non-empty role list.
    /// </summary>
    string[] RequiredAnyRole);
