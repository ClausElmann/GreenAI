-- CanUserAccessProfile.sql
-- Returns 1 if the user has access to the given profile via ProfileUserMappings, 0 otherwise.
-- Access is granted explicitly via the many-to-many mapping — no implicit inheritance.
SELECT
    CAST(CASE WHEN EXISTS (
        SELECT 1
        FROM   [dbo].[ProfileUserMappings]
        WHERE  [UserId]    = @UserId
          AND  [ProfileId] = @ProfileId
    ) THEN 1 ELSE 0 END AS BIT) AS [CanAccess];
