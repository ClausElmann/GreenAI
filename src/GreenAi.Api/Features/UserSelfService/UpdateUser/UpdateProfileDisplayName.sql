UPDATE [dbo].[Profiles]
SET    [DisplayName] = @DisplayName
WHERE  [Id] = @ProfileId
  AND  [CustomerId] = @CustomerId;
