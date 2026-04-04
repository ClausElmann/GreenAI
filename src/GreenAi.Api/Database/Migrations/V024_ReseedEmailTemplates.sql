-- V024: Re-seed EmailTemplates.
-- Data was wiped by Respawn before EmailTemplates was added to TablesToIgnore.
-- This migration uses INSERT … WHERE NOT EXISTS so it is safe to run multiple times.

INSERT INTO [dbo].[EmailTemplates] ([Name], [LanguageId], [Subject], [BodyHtml])
SELECT v.[Name], v.[LanguageId], v.[Subject], v.[BodyHtml]
FROM (VALUES
    ('password-reset', 1,
     'Nulstil dit password',
     N'<p>Hej {{name}},</p><p>Klik på linket nedenfor for at nulstille dit password. Linket er gyldigt i {{ttl}} minutter.</p><p><a href="{{link}}">Nulstil password</a></p><p>Hvis du ikke har bedt om dette, kan du ignorere denne mail.</p>'),
    ('password-reset', 3,
     'Reset your password',
     N'<p>Hi {{name}},</p><p>Click the link below to reset your password. The link is valid for {{ttl}} minutes.</p><p><a href="{{link}}">Reset password</a></p><p>If you did not request this, you can ignore this email.</p>')
) AS v([Name], [LanguageId], [Subject], [BodyHtml])
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[EmailTemplates]
    WHERE [Name] = v.[Name] AND [LanguageId] = v.[LanguageId]
);
