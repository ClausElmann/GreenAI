using GreenAi.Api.SharedKernel.Permissions;
using MudBlazor;

namespace GreenAi.Api.SharedKernel.Navigation;

/// <summary>
/// Static catalog of all navigable pages available in the UI shell.
/// Mirrors nav_visible_for entries in docs/SSOT/ui/models/ui-navigation-schema.json.
/// Keep in sync when new pages with nav_visible_for entries are added.
/// </summary>
public static class NavCatalog
{
    public static readonly IReadOnlyList<NavItem> All = new NavItem[]
    {
        // any_authenticated — no role required
        new("nav.Home",          "/dashboard",      Icons.Material.Filled.Dashboard,      []),
        new("nav.Send",          "/send/wizard",    Icons.Material.Filled.Send,           []),
        new("nav.Status",        "/status",         Icons.Material.Filled.CheckCircle,    []),
        new("nav.Drafts",        "/drafts",         Icons.Material.Filled.Edit,           []),
        new("nav.UserProfile",   "/user/profile",   Icons.Material.Filled.Person,         []),

        // role-gated — nav_visible_for: ["CustomerSetup", "ManageUsers", "ManageProfiles"]
        new("nav.CustomerAdmin", "/customer-admin", Icons.Material.Filled.BusinessCenter,
            [UserRoleNames.CustomerSetup, UserRoleNames.ManageUsers, UserRoleNames.ManageProfiles]),

        // role-gated — nav_visible_for: ["SuperAdmin", "ManageUsers"]
        new("nav.AdminUsers",    "/admin/users",    Icons.Material.Filled.ManageAccounts,
            [UserRoleNames.SuperAdmin, UserRoleNames.ManageUsers]),
    };
}
