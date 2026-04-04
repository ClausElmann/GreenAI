-- Grant an existing user access to a profile via ProfileUserMappings.
-- Idempotent: skips insert if mapping already exists.
INSERT INTO [dbo].[ProfileUserMappings] ([ProfileId], [UserId])
SELECT @ProfileId, @UserId
WHERE NOT EXISTS (
    SELECT 1
    FROM   [dbo].[ProfileUserMappings]
    WHERE  [ProfileId] = @ProfileId
      AND  [UserId]    = @UserId
);
