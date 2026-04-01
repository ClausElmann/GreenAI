-- SelectProfile/SaveRefreshToken.sql
-- Persists a refresh token that carries the resolved ProfileId.
-- ProfileId must be > 0 — the ProfileId(0) placeholder is forbidden from Step 11 onward.
INSERT INTO UserRefreshTokens (CustomerId, UserId, Token, ExpiresAt, LanguageId, ProfileId)
VALUES (@CustomerId, @UserId, @Token, @ExpiresAt, @LanguageId, @ProfileId);
