-- V017 SeedLabels_SharedAndFeature
-- Seeds new shared.* and feature.* localization labels (DA + EN).
-- Depends: V014 (shared labels baseline), V011 (Labels table)
-- Idempotent: skips rows that already exist.

SET NOCOUNT ON;

INSERT INTO [dbo].[Labels] ([ResourceName], [ResourceValue], [LanguageId])
SELECT v.[ResourceName], v.[ResourceValue], v.[LanguageId]
FROM (VALUES
    -- shared.* — generic UI labels
    ('shared.NameLabel',                        'Navn',                                              1),
    ('shared.NameLabel',                        'Name',                                              3),
    ('shared.EmailLabel',                       'E-mail',                                            1),
    ('shared.EmailLabel',                       'Email',                                             3),
    ('shared.ColumnEmail',                      'E-mail',                                            1),
    ('shared.ColumnEmail',                      'Email',                                             3),
    ('shared.Active',                           'Aktiv',                                             1),
    ('shared.Active',                           'Active',                                            3),
    ('shared.Inactive',                         'Inaktiv',                                           1),
    ('shared.Inactive',                         'Inactive',                                          3),
    ('shared.DeletedMessage',                   '{0} slettet.',                                      1),
    ('shared.DeletedMessage',                   '{0} deleted.',                                      3),
    -- nav.* — navigation labels
    ('nav.Home',                                'Forside',                                           1),
    ('nav.Home',                                'Home',                                              3),
    ('nav.CustomerAdmin',                       'Kundestyre',                                        1),
    ('nav.CustomerAdmin',                       'Customer Admin',                                    3),
    -- feature.profile.* — ProfileList component
    ('feature.profile.CreateButton',            'Opret profil',                                      1),
    ('feature.profile.CreateButton',            'Create profile',                                    3),
    ('feature.profile.DeleteTitle',             'Slet profil',                                       1),
    ('feature.profile.DeleteTitle',             'Delete profile',                                    3),
    ('feature.profile.NoRecords',               'Ingen profiler.',                                   1),
    ('feature.profile.NoRecords',               'No profiles.',                                      3),
    ('feature.profile.CreateNotImplemented',    'Opret profil — ikke implementeret endnu.',          1),
    ('feature.profile.CreateNotImplemented',    'Create profile — not yet implemented.',             3),
    -- feature.user.* — UserList component
    ('feature.user.CreateButton',               'Opret bruger',                                      1),
    ('feature.user.CreateButton',               'Create user',                                       3),
    ('feature.user.DeleteTitle',                'Slet bruger',                                       1),
    ('feature.user.DeleteTitle',                'Delete user',                                       3),
    ('feature.user.NoRecords',                  'Ingen brugere.',                                    1),
    ('feature.user.NoRecords',                  'No users.',                                         3),
    ('feature.user.CreateNotImplemented',       'Opret bruger — ikke implementeret endnu.',          1),
    ('feature.user.CreateNotImplemented',       'Create user — not yet implemented.',                3),
    -- feature.settings.* — SettingsTab component
    ('feature.settings.SavedMessage',           'Indstillinger gemt.',                               1),
    ('feature.settings.SavedMessage',           'Settings saved.',                                   3),
    -- shared.* — additional UI labels
    ('shared.OpenButton',                       'Åbn',                                               1),
    ('shared.OpenButton',                       'Open',                                              3),
    ('shared.BackToCustomerAdmin',              'Tilbage til kundestyre',                            1),
    ('shared.BackToCustomerAdmin',              'Back to customer admin',                            3),
    -- page.home.* — Home page
    ('page.home.CustomerAdminDescription',      'Administrer brugere, profiler og indstillinger for din kunde.', 1),
    ('page.home.CustomerAdminDescription',      'Manage users, profiles and settings for your customer.',        3),
    -- feature.customerAdmin.* — CustomerAdmin Index page tabs
    ('feature.customerAdmin.TabSettings',       'Indstillinger',                                     1),
    ('feature.customerAdmin.TabSettings',       'Settings',                                          3),
    ('feature.customerAdmin.TabUsers',          'Brugere',                                           1),
    ('feature.customerAdmin.TabUsers',          'Users',                                             3),
    ('feature.customerAdmin.TabProfiles',       'Profiler',                                          1),
    ('feature.customerAdmin.TabProfiles',       'Profiles',                                          3),
    -- feature.profile.* — ProfileDetail page
    ('feature.profile.NotFound',                'Profil ikke fundet (id: {0}).',                     1),
    ('feature.profile.NotFound',                'Profile not found (id: {0}).',                      3),
    ('feature.profile.NameLabel',               'Profilnavn',                                        1),
    ('feature.profile.NameLabel',               'Profile name',                                      3),
    ('feature.profile.BreadcrumbDefault',       'Profil {0}',                                        1),
    ('feature.profile.BreadcrumbDefault',       'Profile {0}',                                       3),
    -- feature.user.* — UserDetail page
    ('feature.user.NotFound',                   'Bruger ikke fundet (id: {0}).',                     1),
    ('feature.user.NotFound',                   'User not found (id: {0}).',                         3),
    ('feature.user.TabProfiles',                'Profiler',                                          1),
    ('feature.user.TabProfiles',                'Profiles',                                          3),
    ('feature.user.TabRoles',                   'Roller',                                            1),
    ('feature.user.TabRoles',                   'Roles',                                             3),
    ('feature.user.NoProfilesAssigned',         'Ingen profiler tildelt.',                           1),
    ('feature.user.NoProfilesAssigned',         'No profiles assigned.',                             3),
    ('feature.user.ColumnProfile',              'Profil',                                            1),
    ('feature.user.ColumnProfile',              'Profile',                                           3),
    ('feature.user.ColumnRole',                 'Rolle',                                             1),
    ('feature.user.ColumnRole',                 'Role',                                              3),
    ('feature.user.NoRolesAssigned',            'Ingen roller tildelt.',                             1),
    ('feature.user.NoRolesAssigned',            'No roles assigned.',                                3),
    ('feature.user.BreadcrumbDefault',          'Bruger {0}',                                        1),
    ('feature.user.BreadcrumbDefault',          'User {0}',                                          3)
) AS v ([ResourceName], [ResourceValue], [LanguageId])
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Labels] l
    WHERE l.[ResourceName] = v.[ResourceName]
      AND l.[LanguageId]   = v.[LanguageId]
);
