-- V025 SeedLabels_Portal
-- Seeds navigation and UI labels for P2 Portal Core pages (DA + EN).
-- Idempotent: skips rows that already exist.

SET NOCOUNT ON;

INSERT INTO [dbo].[Labels] ([ResourceName], [ResourceValue], [LanguageId])
SELECT v.[ResourceName], v.[ResourceValue], v.[LanguageId]
FROM (VALUES
    -- nav.* — P2 portal navigation labels
    ('nav.UserProfile',                         'Min profil',                                        1),
    ('nav.UserProfile',                         'My Profile',                                        3),
    ('nav.AdminUsers',                          'Brugerstyring',                                     1),
    ('nav.AdminUsers',                          'User Management',                                   3),
    ('nav.AdminSettings',                       'Systemindstillinger',                               1),
    ('nav.AdminSettings',                       'System Settings',                                   3),

    -- feature.userProfile.* — UserProfilePage
    ('feature.userProfile.Title',               'Min profil',                                        1),
    ('feature.userProfile.Title',               'My Profile',                                        3),
    ('feature.userProfile.DisplayNameLabel',    'Visningsnavn',                                      1),
    ('feature.userProfile.DisplayNameLabel',    'Display Name',                                      3),
    ('feature.userProfile.LanguageLabel',       'Sprog',                                             1),
    ('feature.userProfile.LanguageLabel',       'Language',                                          3),
    ('feature.userProfile.SaveButton',          'Gem ændringer',                                     1),
    ('feature.userProfile.SaveButton',          'Save Changes',                                      3),
    ('feature.userProfile.SaveSuccess',         'Profil opdateret.',                                 1),
    ('feature.userProfile.SaveSuccess',         'Profile updated.',                                  3),

    -- feature.adminUsers.* — AdminUserListPage
    ('feature.adminUsers.Title',                'Brugerstyring',                                     1),
    ('feature.adminUsers.Title',                'User Management',                                   3),
    ('feature.adminUsers.CreateButton',         'Opret bruger',                                      1),
    ('feature.adminUsers.CreateButton',         'Create User',                                       3),
    ('feature.adminUsers.EmailColumn',          'E-mail',                                            1),
    ('feature.adminUsers.EmailColumn',          'Email',                                             3),
    ('feature.adminUsers.ActionsColumn',        'Handlinger',                                        1),
    ('feature.adminUsers.ActionsColumn',        'Actions',                                           3),

    -- feature.adminSettings.* — AdminSettingsPage
    ('feature.adminSettings.Title',             'Systemindstillinger',                               1),
    ('feature.adminSettings.Title',             'System Settings',                                   3),
    ('feature.adminSettings.KeyColumn',         'Nøgle',                                             1),
    ('feature.adminSettings.KeyColumn',         'Key',                                               3),
    ('feature.adminSettings.ValueColumn',       'Værdi',                                             1),
    ('feature.adminSettings.ValueColumn',       'Value',                                             3),
    ('feature.adminSettings.SaveButton',        'Gem',                                               1),
    ('feature.adminSettings.SaveButton',        'Save',                                              3),
    ('feature.adminSettings.SaveSuccess',       'Indstillingen er gemt.',                            1),
    ('feature.adminSettings.SaveSuccess',       'Setting saved.',                                    3)
) AS v ([ResourceName], [ResourceValue], [LanguageId])
WHERE NOT EXISTS (
    SELECT 1
    FROM [dbo].[Labels] existing
    WHERE existing.[ResourceName] = v.[ResourceName]
      AND existing.[LanguageId]   = v.[LanguageId]
);
