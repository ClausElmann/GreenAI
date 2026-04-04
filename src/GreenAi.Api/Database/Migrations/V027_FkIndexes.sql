-- V027: Add missing indexes on FK columns
-- All queries against tenant-owned tables filter by CustomerId or UserId.
-- Missing indexes cause full table scans as data grows.
-- Idempotent: IF NOT EXISTS guards on all index creations.

-- Profiles.CustomerId — used in every tenant-scoped profile query
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Profiles') AND name = 'IX_Profiles_CustomerId')
    CREATE INDEX [IX_Profiles_CustomerId] ON [dbo].[Profiles] ([CustomerId]);

-- UserCustomerMemberships.CustomerId — used in membership lookups (login, select-customer)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.UserCustomerMemberships') AND name = 'IX_UserCustomerMemberships_CustomerId')
    CREATE INDEX [IX_UserCustomerMemberships_CustomerId] ON [dbo].[UserCustomerMemberships] ([CustomerId]);

-- UserRefreshTokens.UserId — used in logout (DELETE WHERE UserId = @UserId)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.UserRefreshTokens') AND name = 'IX_UserRefreshTokens_UserId')
    CREATE INDEX [IX_UserRefreshTokens_UserId] ON [dbo].[UserRefreshTokens] ([UserId]);

-- UserRefreshTokens.CustomerId — used in refresh token queries scoped by customer
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.UserRefreshTokens') AND name = 'IX_UserRefreshTokens_CustomerId')
    CREATE INDEX [IX_UserRefreshTokens_CustomerId] ON [dbo].[UserRefreshTokens] ([CustomerId]);

-- PasswordResetTokens.UserId — used in token validation (SELECT WHERE UserId + Token)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.PasswordResetTokens') AND name = 'IX_PasswordResetTokens_UserId')
    CREATE INDEX [IX_PasswordResetTokens_UserId] ON [dbo].[PasswordResetTokens] ([UserId]);

-- ProfileUserMappings.UserId — used in profile availability queries (GetAvailableProfiles WHERE UserId)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ProfileUserMappings') AND name = 'IX_ProfileUserMappings_UserId')
    CREATE INDEX [IX_ProfileUserMappings_UserId] ON [dbo].[ProfileUserMappings] ([UserId]);

-- AuditLog.ActorId — used in audit trail queries by actor
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.AuditLog') AND name = 'IX_AuditLog_ActorId')
    CREATE INDEX [IX_AuditLog_ActorId] ON [dbo].[AuditLog] ([ActorId]);

-- EmailTemplates.LanguageId — used in template lookup (SELECT WHERE Name + LanguageId)
-- Covered by UIX_EmailTemplates_Name_LanguageId composite unique — no separate index needed.
-- CustomerUserRoleMappings, Countries, ProfileRoleMappings — lookup tables, rarely queried by FK alone.
