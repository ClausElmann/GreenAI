using GreenAi.Api.SharedKernel.Permissions;
using MudBlazor;

namespace GreenAi.Api.SharedKernel.Navigation;

/// <summary>
/// Static catalog of all navigable pages available in the UI shell.
/// Mirrors nav_visible_for entries in docs/SSOT/ui/ui-architecture-decisions.md.
/// Keep in sync when new pages with nav_visible_for entries are added.
/// </summary>
public static class NavCatalog
{
    public static readonly IReadOnlyList<NavItem> All = new NavItem[]
    {
        // any_authenticated — no role required
        new("nav.Broadcasting", "/broadcasting",   Icons.Material.Outlined.Hub,          []),
        new("nav.Status",       "/status",          Icons.Material.Filled.CheckCircle,    []),
        new("nav.Drafts",       "/drafts",          Icons.Material.Filled.Edit,           []),
        new("nav.UserProfile",  "/user/profile",    Icons.Material.Filled.Person,         []),

        // role-gated — nav_visible_for: ["CustomerSetup", "ManageUsers", "ManageProfiles"]
        new("nav.CustomerAdmin", "/customer-admin", Icons.Material.Filled.BusinessCenter,
            [UserRoleNames.CustomerSetup, UserRoleNames.ManageUsers, UserRoleNames.ManageProfiles]),

        // role-gated — nav_visible_for: ["SuperAdmin", "ManageUsers"]
        new("nav.AdminUsers",   "/admin/users",     Icons.Material.Filled.ManageAccounts,
            [UserRoleNames.SuperAdmin, UserRoleNames.ManageUsers]),

        // role-gated — SuperAdmin only
        new("nav.AdminSettings", "/admin/settings", Icons.Material.Filled.Settings,
            [UserRoleNames.SuperAdmin]),
        new("nav.SuperAdmin",    "/admin/super",    Icons.Material.Filled.AdminPanelSettings,
            [UserRoleNames.SuperAdmin]),
    };
}
