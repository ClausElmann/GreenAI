UPDATE UserRefreshTokens
SET UsedAt = @UsedAt
WHERE Id = @TokenId;
