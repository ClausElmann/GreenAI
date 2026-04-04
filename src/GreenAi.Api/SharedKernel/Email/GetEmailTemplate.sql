-- Returns the best-match template for the given name + languageId.
-- Falls back to EN (LanguageId=3) if language not found.
SELECT TOP 1
    [Id],
    [Name],
    [LanguageId],
    [Subject],
    [BodyHtml]
FROM [dbo].[EmailTemplates]
WHERE [Name] = @Name
  AND [LanguageId] IN (@LanguageId, 3)
ORDER BY
    CASE WHEN [LanguageId] = @LanguageId THEN 0 ELSE 1 END;
