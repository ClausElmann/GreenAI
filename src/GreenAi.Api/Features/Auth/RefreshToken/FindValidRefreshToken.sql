SELECT
    t.Id,
    t.UserId,
    t.CustomerId,
    t.LanguageId,
    t.ProfileId,
    u.Email
FROM UserRefreshTokens t
INNER JOIN Users u ON u.Id = t.UserId
WHERE t.Token    = @Token
  AND t.UsedAt   IS NULL
  AND t.ExpiresAt > @UtcNow;
