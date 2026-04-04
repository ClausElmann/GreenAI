SELECT p.[DisplayName]  AS ProfileDisplayName,
       c.[Name]         AS CustomerName
FROM   [dbo].[Profiles]  p
JOIN   [dbo].[Customers] c ON c.[Id] = p.[CustomerId]
WHERE  p.[Id]         = @ProfileId
  AND  p.[CustomerId] = @CustomerId
