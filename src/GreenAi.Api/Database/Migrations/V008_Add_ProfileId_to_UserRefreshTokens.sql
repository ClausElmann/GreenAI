-- V008: Add ProfileId to UserRefreshTokens
-- Tracks the resolved Profiles.Id at the time the token was issued.
-- DEFAULT 0 covers pre-Step-11 rows ONLY. Application code must never INSERT ProfileId = 0
-- from Step 11 onward. ProfileId(0) is forbidden at runtime (PROFILE_CORE_DOMAIN decision).
ALTER TABLE UserRefreshTokens
ADD ProfileId INT NOT NULL DEFAULT 0;
