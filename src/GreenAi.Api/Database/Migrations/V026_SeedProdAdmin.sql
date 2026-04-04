-- V026 SeedProdAdmin
-- Seeds production admin user: claus.elmann@gmail.com
-- Creates customer "LumiFarms", links user as SuperAdmin with a default profile.
-- Idempotent: all inserts guarded by IF NOT EXISTS.

SET NOCOUNT ON;

-- -------------------------------------------------------------------------
-- Customer: GreenAI (Id=1 — first insert on fresh DB)
-- -------------------------------------------------------------------------
DECLARE @CustomerId INT;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers] WHERE [Name] = 'GreenAI')
BEGIN
    SET IDENTITY_INSERT [dbo].[Customers] ON;
    INSERT INTO [dbo].[Customers] ([Id], [Name])
    VALUES (1, 'GreenAI');
    SET IDENTITY_INSERT [dbo].[Customers] OFF;
END;

SELECT @CustomerId = [Id] FROM [dbo].[Customers] WHERE [Name] = 'GreenAI';

-- -------------------------------------------------------------------------
-- User: claus.elmann@gmail.com
-- Password: Flipper12# (PBKDF2/SHA-512, 100 000 iterations, 64-byte hash)
-- -------------------------------------------------------------------------
DECLARE @UserId INT;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'claus.elmann@gmail.com')
BEGIN
    INSERT INTO [dbo].[Users] ([Email], [PasswordHash], [PasswordSalt], [IsActive])
    VALUES (
        'claus.elmann@gmail.com',
        'N9p1t00iogUQwhDCgeFGRQgv174X9Wjc+NKjIg7g7LdHVGGBtrK88r5jwRsfM7bQszVQV9+333ASHfJ8qKjAhg==',
        'VlN9lBRMfoASx0x6+OrUpbA0TTHXi/X8cEpXU2mYauk=',
        1
    );
END;

SELECT @UserId = [Id] FROM [dbo].[Users] WHERE [Email] = 'claus.elmann@gmail.com';

-- -------------------------------------------------------------------------
-- Membership
-- -------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserCustomerMemberships] WHERE [UserId] = @UserId AND [CustomerId] = @CustomerId)
    INSERT INTO [dbo].[UserCustomerMemberships] ([UserId], [CustomerId], [LanguageId])
    VALUES (@UserId, @CustomerId, 1);

-- -------------------------------------------------------------------------
-- Roles: SuperAdmin
-- -------------------------------------------------------------------------
DECLARE @RoleSuperAdmin INT = (SELECT [Id] FROM [dbo].[UserRoles] WHERE [Name] = 'SuperAdmin');

IF @RoleSuperAdmin IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM [dbo].[UserRoleMappings] WHERE [UserId] = @UserId AND [UserRoleId] = @RoleSuperAdmin)
    INSERT INTO [dbo].[UserRoleMappings] ([UserId], [UserRoleId]) VALUES (@UserId, @RoleSuperAdmin);

-- -------------------------------------------------------------------------
-- Profile: Claus Elmann
-- -------------------------------------------------------------------------
DECLARE @ProfileId INT;

IF NOT EXISTS (SELECT 1 FROM [dbo].[Profiles] WHERE [CustomerId] = @CustomerId AND [DisplayName] = 'Claus Elmann')
BEGIN
    INSERT INTO [dbo].[Profiles] ([CustomerId], [DisplayName])
    VALUES (@CustomerId, 'Claus Elmann');
END;

SELECT @ProfileId = [Id] FROM [dbo].[Profiles] WHERE [CustomerId] = @CustomerId AND [DisplayName] = 'Claus Elmann';

-- -------------------------------------------------------------------------
-- Profile-User mapping
-- -------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[ProfileUserMappings] WHERE [UserId] = @UserId AND [ProfileId] = @ProfileId)
    INSERT INTO [dbo].[ProfileUserMappings] ([UserId], [ProfileId])
    VALUES (@UserId, @ProfileId);
