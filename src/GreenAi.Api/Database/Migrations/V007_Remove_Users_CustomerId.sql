-- V007 Remove_Users_CustomerId
-- Users.CustomerId was a single-tenant artifact: one user, one customer.
-- UserCustomerMembership (V004) replaces this relationship with the multi-tenant model.
-- V005 backfilled all User→Customer relationships into UserCustomerMembership.
-- This migration removes the now-redundant column and its FK constraint.
-- Safe to run: LoginHandler and all auth flows already read CustomerId from
-- UserCustomerMembership — not from this column.
-- Idempotent: IF EXISTS guards prevent failure on re-run.

-- Step 1: Drop the FK constraint (auto-named; discover dynamically)
DECLARE @ConstraintName NVARCHAR(256);
SELECT @ConstraintName = fk.[name]
FROM sys.foreign_keys fk
WHERE fk.parent_object_id      = OBJECT_ID('dbo.Users')
  AND fk.referenced_object_id  = OBJECT_ID('dbo.Customers');

IF @ConstraintName IS NOT NULL
    EXEC ('ALTER TABLE [dbo].[Users] DROP CONSTRAINT [' + @ConstraintName + ']');

-- Step 2: Drop the column
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'CustomerId'
)
    ALTER TABLE [dbo].[Users] DROP COLUMN [CustomerId];
