-- V009: Introduce ProfileUserMappings — many-to-many between Profiles and Users
--
-- The V001 baseline placed a direct UserId FK on Profiles, encoding single-user ownership.
-- This migration transitions to the correct model:
--   - A Profile belongs to exactly one Customer.
--   - Access is governed by ProfileUserMappings (ProfileId, UserId).
--   - A user may be mapped to 0..N profiles across 0..N customers.
--   - A profile may be accessed by many users.
--
-- Steps:
--   1. Create dbo.ProfileUserMappings with PK (ProfileId, UserId) and FK constraints.
--   2. Backfill from existing Profiles.UserId rows (preserves all current access grants).
--   3. Drop FK constraint on Profiles.UserId (auto-named; discovered dynamically).
--   4. Drop Profiles.UserId column.
--
-- Idempotent: IF EXISTS guards on all phases.

-- -----------------------------------------------------------------------
-- Step 1: Create ProfileUserMappings
-- -----------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.ProfileUserMappings')
)
BEGIN
    CREATE TABLE [dbo].[ProfileUserMappings] (
        [ProfileId]  INT  NOT NULL,
        [UserId]     INT  NOT NULL,
        CONSTRAINT [PK_ProfileUserMappings] PRIMARY KEY CLUSTERED ([ProfileId] ASC, [UserId] ASC),
        CONSTRAINT [FK_ProfileUserMappings_Profiles] FOREIGN KEY ([ProfileId]) REFERENCES [dbo].[Profiles] ([Id]),
        CONSTRAINT [FK_ProfileUserMappings_Users]    FOREIGN KEY ([UserId])    REFERENCES [dbo].[Users]    ([Id])
    );
END;

-- -----------------------------------------------------------------------
-- Step 2: Backfill from Profiles.UserId (only if column still exists)
-- -----------------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Profiles') AND name = 'UserId'
)
BEGIN
    INSERT INTO [dbo].[ProfileUserMappings] ([ProfileId], [UserId])
    SELECT p.[Id], p.[UserId]
    FROM   [dbo].[Profiles] p
    WHERE  NOT EXISTS (
        SELECT 1 FROM [dbo].[ProfileUserMappings] m
        WHERE m.[ProfileId] = p.[Id] AND m.[UserId] = p.[UserId]
    );
END;

-- -----------------------------------------------------------------------
-- Step 3: Drop FK constraint on Profiles.UserId (auto-named — discover dynamically)
-- -----------------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Profiles') AND name = 'UserId'
)
BEGIN
    DECLARE @ConstraintName NVARCHAR(256);
    SELECT @ConstraintName = fk.[name]
    FROM sys.foreign_keys fk
    WHERE fk.parent_object_id     = OBJECT_ID('dbo.Profiles')
      AND fk.referenced_object_id = OBJECT_ID('dbo.Users');

    IF @ConstraintName IS NOT NULL
        EXEC ('ALTER TABLE [dbo].[Profiles] DROP CONSTRAINT [' + @ConstraintName + ']');
END;

-- -----------------------------------------------------------------------
-- Step 4: Drop Profiles.UserId column
-- -----------------------------------------------------------------------
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Profiles') AND name = 'UserId'
)
    ALTER TABLE [dbo].[Profiles] DROP COLUMN [UserId];
