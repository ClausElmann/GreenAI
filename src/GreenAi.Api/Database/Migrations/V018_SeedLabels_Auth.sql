-- V018_SeedLabels_Auth.sql
-- Seeds localization labels for the Login page and Profile detail tab.
-- ILocalizationContext is fail-open: if key not found, returns key string itself.

INSERT INTO [dbo].[Labels] ([ResourceName], [ResourceValue], [LanguageId])
SELECT v.[ResourceName], v.[ResourceValue], v.[LanguageId]
FROM (VALUES
    -- Login page
    ('auth.Login.Title',        'Log ind',       1),
    ('auth.Login.Title',        'Sign in',        3),
    ('auth.Login.PasswordLabel','Adgangskode',   1),
    ('auth.Login.PasswordLabel','Password',       3),
    ('auth.Login.Loading',      'Logger ind...', 1),
    ('auth.Login.Loading',      'Signing in...', 3),
    ('auth.Login.SubmitButton', 'Log ind',       1),
    ('auth.Login.SubmitButton', 'Sign in',        3),
    -- Profile detail tabs
    ('feature.profile.InfoTab', 'Info',          1),
    ('feature.profile.InfoTab', 'Info',           3)
) AS v ([ResourceName], [ResourceValue], [LanguageId])
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Labels] l
    WHERE l.[ResourceName] = v.[ResourceName] AND l.[LanguageId] = v.[LanguageId]
);
