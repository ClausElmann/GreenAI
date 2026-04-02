-- BatchUpsertLabels.sql
-- Upserts a single label row: inserts if (ResourceName, LanguageId) does not exist,
-- updates ResourceValue if it does.
-- Called once per label via Dapper ExecuteAsync in a loop (simple, auditable, no TVP dependency).

MERGE [dbo].[Labels] AS target
USING (SELECT @ResourceName AS [ResourceName], @LanguageId AS [LanguageId]) AS source
    ON target.[ResourceName] = source.[ResourceName]
   AND target.[LanguageId]   = source.[LanguageId]
WHEN MATCHED THEN
    UPDATE SET [ResourceValue] = @ResourceValue
WHEN NOT MATCHED THEN
    INSERT ([ResourceName], [ResourceValue], [LanguageId])
    VALUES (@ResourceName,  @ResourceValue,  @LanguageId);
