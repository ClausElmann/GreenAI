SELECT Id, DisplayName AS Name, CAST(1 AS BIT) AS IsActive
FROM Profiles
WHERE Id = @ProfileId
  AND CustomerId = @CustomerId;
