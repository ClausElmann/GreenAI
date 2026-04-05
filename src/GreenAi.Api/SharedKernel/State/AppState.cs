using GreenAi.Api.SharedKernel.Auth;
using GreenAi.Api.SharedKernel.Navigation;

namespace GreenAi.Api.SharedKernel.State;

/// <summary>
/// Scoped UI state service for the current Blazor circuit.
/// Holds overlay/nav open state — no business logic.
///
/// Usage:
///   @inject AppState AppState
///   AppState.ToggleOverlayNav();
///   AppState.StateChanged += StateHasChanged;
/// </summary>
public sealed class AppState
{
    /// <summary>Whether the OverlayNav panel is open.</summary>
    public bool IsOverlayNavOpen { get; private set; }

    /// <summary>Whether the CommandPalette modal is open (not yet implemented).</summary>
    public bool IsCommandPaletteOpen { get; private set; }

    /// <summary>
    /// Customers available for selection after login (multi-customer accounts).
    /// Cleared after customer is selected.
    /// </summary>
    public IReadOnlyList<CustomerSummary> PendingCustomers { get; private set; } = [];

    /// <summary>
    /// Profiles available for selection (single-customer/multi-profile or after customer selection).
    /// Cleared after profile is selected.
    /// </summary>
    public IReadOnlyList<ProfileSummary> PendingProfiles { get; private set; } = [];

    /// <summary>Display name of the active profile. Set after login. Null until context is loaded.</summary>
    public string? ActiveProfileName { get; private set; }

    /// <summary>Name of the active customer. Set after login. Null until context is loaded.</summary>
    public string? ActiveCustomerName { get; private set; }

    /// <summary>
    /// Nav items the current user is permitted to access.
    /// Populated by MainLayout after authentication and role resolution.
    /// Empty until the auth context is loaded — Command Palette will show no results until then.
    /// </summary>
    public IReadOnlyList<NavItem> AllowedNavItems { get; private set; } = [];

    /// <summary>Fired whenever any state property changes — subscribers call StateHasChanged().</summary>
    public event Action? StateChanged;

    public void ToggleOverlayNav()
    {
        IsOverlayNavOpen = !IsOverlayNavOpen;
        StateChanged?.Invoke();
    }

    public void CloseOverlayNav()
    {
        if (!IsOverlayNavOpen) return;
        IsOverlayNavOpen = false;
        StateChanged?.Invoke();
    }

    public void OpenCommandPalette()
    {
        if (IsCommandPaletteOpen) return;
        IsCommandPaletteOpen = true;
        StateChanged?.Invoke();
    }

    public void CloseCommandPalette()
    {
        if (!IsCommandPaletteOpen) return;
        IsCommandPaletteOpen = false;
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Sets the active profile and customer display names shown in TopBar.
    /// Called from MainLayout after the profile context query succeeds.
    /// </summary>
    public void SetActiveContext(string? profileName, string? customerName)
    {
        ActiveProfileName  = profileName;
        ActiveCustomerName = customerName;
        StateChanged?.Invoke();
    }

    /// <summary>Stores customers available for selection and fires StateChanged.</summary>
    public void SetPendingCustomers(IReadOnlyList<CustomerSummary> customers)
    {
        PendingCustomers = customers;
        StateChanged?.Invoke();
    }

    /// <summary>Stores profiles available for selection and fires StateChanged.</summary>
    public void SetPendingProfiles(IReadOnlyList<ProfileSummary> profiles)
    {
        PendingProfiles = profiles;
        StateChanged?.Invoke();
    }

    /// <summary>Clears pending selection state after a context has been fully resolved.</summary>
    public void ClearPendingSelection()
    {
        PendingCustomers = [];
        PendingProfiles  = [];
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Sets the nav items the current user is permitted to navigate to.
    /// Called from MainLayout after role resolution. Read by CommandPalette.
    /// </summary>
    public void SetAllowedNavItems(IReadOnlyList<NavItem> items)
    {
        AllowedNavItems = items;
        StateChanged?.Invoke();
    }
}
