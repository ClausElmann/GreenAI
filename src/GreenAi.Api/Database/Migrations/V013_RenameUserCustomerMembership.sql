-- V013 RenameUserCustomerMembership
-- Renames dbo.UserCustomerMembership → dbo.UserCustomerMemberships
-- to align with the plural naming convention (always plural, no exceptions).
-- Historical migrations V004 and V005 retain the old name as permanent record.
-- Idempotent: skips all steps if old table no longer exists (already renamed).

IF OBJECT_ID('dbo.UserCustomerMembership', 'U') IS NOT NULL
BEGIN
    -- Step 1: Drop dependent indexes and constraints
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.UserCustomerMembership') AND name = 'UIX_UserCustomerMembership_UserId_CustomerId')
        DROP INDEX [UIX_UserCustomerMembership_UserId_CustomerId] ON [dbo].[UserCustomerMembership];

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('dbo.UserCustomerMembership') AND name = 'FK_UserCustomerMembership_Customers')
        ALTER TABLE [dbo].[UserCustomerMembership] DROP CONSTRAINT [FK_UserCustomerMembership_Customers];

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('dbo.UserCustomerMembership') AND name = 'FK_UserCustomerMembership_Users')
        ALTER TABLE [dbo].[UserCustomerMembership] DROP CONSTRAINT [FK_UserCustomerMembership_Users];

    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('dbo.UserCustomerMembership') AND name = 'PK_UserCustomerMembership')
        ALTER TABLE [dbo].[UserCustomerMembership] DROP CONSTRAINT [PK_UserCustomerMembership];

    -- Step 2: Rename the table
    EXEC sp_rename 'dbo.UserCustomerMembership', 'UserCustomerMemberships';

    -- Step 3: Recreate constraints and index with new names
    ALTER TABLE [dbo].[UserCustomerMemberships]
        ADD CONSTRAINT [PK_UserCustomerMemberships] PRIMARY KEY CLUSTERED ([Id] ASC);

    ALTER TABLE [dbo].[UserCustomerMemberships]
        ADD CONSTRAINT [FK_UserCustomerMemberships_Users]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]);

    ALTER TABLE [dbo].[UserCustomerMemberships]
        ADD CONSTRAINT [FK_UserCustomerMemberships_Customers]
        FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers] ([Id]);

    CREATE UNIQUE NONCLUSTERED INDEX [UIX_UserCustomerMemberships_UserId_CustomerId]
        ON [dbo].[UserCustomerMemberships] ([UserId] ASC, [CustomerId] ASC);
END;
