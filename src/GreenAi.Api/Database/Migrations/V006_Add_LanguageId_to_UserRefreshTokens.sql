-- V006 Add_LanguageId_to_UserRefreshTokens
-- Persists LanguageId in the refresh token row so token rotation preserves
-- the full identity context without a new membership lookup.
-- Additive only: does not modify other tables.

ALTER TABLE [dbo].[UserRefreshTokens]
    ADD [LanguageId] INT NOT NULL DEFAULT 1;
