-- V022 EmailTemplates
-- Template-based transactional email system.
-- Templates are stored per language — fallback to LanguageId=3 (EN) if language not found.
-- Body supports {{token}}, {{name}}, {{link}} substitution (handled in EmailTemplateRenderer).
-- Idempotent: IF NOT EXISTS guard.

IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.EmailTemplates')
)
BEGIN
    CREATE TABLE [dbo].[EmailTemplates] (
        [Id]         INT              IDENTITY(1,1) PRIMARY KEY,
        [Name]       NVARCHAR(100)    NOT NULL,
        [LanguageId] INT              NOT NULL REFERENCES [dbo].[Languages]([Id]),
        [Subject]    NVARCHAR(500)    NOT NULL,
        [BodyHtml]   NVARCHAR(MAX)    NOT NULL,
        [UpdatedAt]  DATETIMEOFFSET   NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );

    CREATE UNIQUE INDEX [UIX_EmailTemplates_Name_LanguageId]
        ON [dbo].[EmailTemplates] ([Name], [LanguageId]);
END;

-- Seed: Password Reset template in DA (1) and EN (3)
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
