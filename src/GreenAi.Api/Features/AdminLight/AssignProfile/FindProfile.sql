-- Verify profile belongs to the caller's customer (tenant isolation)
SELECT COUNT(1)
FROM   [dbo].[Profiles]
WHERE  [Id]         = @ProfileId
  AND  [CustomerId] = @CustomerId;
