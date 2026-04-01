-- CustomerUserRoleMappings — POLICY table.
-- Links a UserRole to a Customer as an available/configurable role.
-- IMPORTANT: There is NO UserId column. This is NOT a per-user role assignment.
-- It defines which UserRoles a customer chooses to activate in its admin UI.
-- Actual per-user role assignments are in UserRoleMappings (UserId, UserRoleId).
CREATE TABLE [dbo].[CustomerUserRoleMappings] (
    [CustomerId]  INT  NOT NULL,
    [UserRoleId]  INT  NOT NULL,
    CONSTRAINT [PK_CustomerUserRoleMappings] PRIMARY KEY CLUSTERED ([CustomerId] ASC, [UserRoleId] ASC),
    CONSTRAINT [FK_CustomerUserRoleMappings_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers]  ([Id]),
    CONSTRAINT [FK_CustomerUserRoleMappings_UserRoles] FOREIGN KEY ([UserRoleId]) REFERENCES [dbo].[UserRoles]   ([Id])
);
