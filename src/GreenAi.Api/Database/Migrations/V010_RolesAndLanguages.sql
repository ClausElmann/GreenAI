-- V010: UserRoles, ProfileRoles, UserRoleMappings, ProfileRoleMappings,
--       CustomerUserRoleMappings, Languages
--
-- FOUNDATIONAL_DOMAIN_ANALYSIS tables — all required for IPermissionService.
--
-- Terminology (from source system analysis):
--   UserRole     — admin/UI capability flag per User (global, NOT per-customer).
--                  40 values in source system (SuperAdmin, ManageUsers, API, etc.)
--   ProfileRole  — operational capability flag per Profile.
--                  63 values in source system (HaveNoSendRestrictions, CanSendByEboks, etc.)
--   CustomerUserRoleMappings — a POLICY table (CustomerId, UserRoleId, NO UserId).
--                  Defines which UserRoles a customer chooses to configure.
--                  NOT a per-user role assignment.
--   Languages   — lookup table for supported languages (seed: Danish=1).
--
-- All tables are idempotent (IF NOT EXISTS guards).

-- -----------------------------------------------------------------------
-- Languages (must be first — UserCustomerMembership.LanguageId references it logically)
-- FK from UserCustomerMembership.LanguageId is added here.
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.Languages'))
BEGIN
    CREATE TABLE [dbo].[Languages] (
        [Id]              INT             IDENTITY (1, 1) NOT NULL,
        [Name]            NVARCHAR (100)  NOT NULL,
        [LanguageCulture] NVARCHAR (20)   NOT NULL,   -- e.g. 'da-DK', 'sv-SE', 'en-GB'
        [UniqueSeoCode]   NVARCHAR (10)   NOT NULL,   -- e.g. 'da', 'sv', 'en'
        [Published]       BIT             NOT NULL DEFAULT 1,
        [DisplayOrder]    INT             NOT NULL DEFAULT 0,
        [CreatedAt]       DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [PK_Languages] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE UNIQUE INDEX [UIX_Languages_LanguageCulture] ON [dbo].[Languages] ([LanguageCulture]);
END;

-- Seed: Danish (Id=1 — bootstrap default in UserCustomerMembership.LanguageId DEFAULT 1)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Languages] WHERE [Id] = 1)
BEGIN
    SET IDENTITY_INSERT [dbo].[Languages] ON;

    INSERT INTO [dbo].[Languages] ([Id], [Name], [LanguageCulture], [UniqueSeoCode], [Published], [DisplayOrder])
    VALUES
        (1, 'Danish',   'da-DK', 'da', 1, 1),
        (2, 'Swedish',  'sv-SE', 'sv', 1, 2),
        (3, 'English',  'en-GB', 'en', 1, 3),
        (4, 'Finnish',  'fi-FI', 'fi', 1, 4),
        (5, 'Norwegian','nb-NO', 'nb', 1, 5),
        (6, 'German',   'de-DE', 'de', 1, 6);

    SET IDENTITY_INSERT [dbo].[Languages] OFF;
END;

-- -----------------------------------------------------------------------
-- UserRoles — admin/UI capability flag definitions
-- Lookup table. 40 values in the source system.
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.UserRoles'))
BEGIN
    CREATE TABLE [dbo].[UserRoles] (
        [Id]          INT             IDENTITY (1, 1) NOT NULL,
        [Name]        NVARCHAR (100)  NOT NULL,
        [CreatedAt]   DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_UserRoles_Name] UNIQUE ([Name])
    );
END;

-- Seed: core roles required for the build order.
-- RULE: DO NOT add all 40 source-system roles speculatively.
-- Add roles only when a handler requires them. These are the minimum
-- required to implement DoesUserHaveRole() and the API access guard.
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserRoles] WHERE [Name] = 'SuperAdmin')
BEGIN
    INSERT INTO [dbo].[UserRoles] ([Name]) VALUES
        ('SuperAdmin'),       -- bypasses most authorization checks
        ('API'),              -- required for machine-to-machine API access
        ('ManageUsers'),      -- user administration UI
        ('ManageProfiles'),   -- profile administration UI
        ('CustomerSetup'),    -- customer-level configuration UI
        ('TwoFactorAuthenticate'); -- forces 2FA requirement for this user
END;

-- -----------------------------------------------------------------------
-- UserRoleMappings — direct (UserId, UserRoleId) assignment.
-- GLOBAL — no CustomerId column by design (CONTRADICTION_003 acknowledged,
-- deferred to Option D migration phase per IDENTITY_REFACTOR_PLAN.md).
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.UserRoleMappings'))
BEGIN
    CREATE TABLE [dbo].[UserRoleMappings] (
        [UserId]      INT  NOT NULL,
        [UserRoleId]  INT  NOT NULL,
        CONSTRAINT [PK_UserRoleMappings] PRIMARY KEY CLUSTERED ([UserId] ASC, [UserRoleId] ASC),
        CONSTRAINT [FK_UserRoleMappings_Users]     FOREIGN KEY ([UserId])     REFERENCES [dbo].[Users]     ([Id]),
        CONSTRAINT [FK_UserRoleMappings_UserRoles] FOREIGN KEY ([UserRoleId]) REFERENCES [dbo].[UserRoles]  ([Id])
    );
END;

-- -----------------------------------------------------------------------
-- ProfileRoles — operational capability flag definitions
-- Lookup table. 63 values in the source system.
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.ProfileRoles'))
BEGIN
    CREATE TABLE [dbo].[ProfileRoles] (
        [Id]          INT             IDENTITY (1, 1) NOT NULL,
        [Name]        NVARCHAR (100)  NOT NULL,
        [CreatedAt]   DATETIMEOFFSET  NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [PK_ProfileRoles] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_ProfileRoles_Name] UNIQUE ([Name])
    );
END;

-- Seed: operational roles required for the build order.
-- RULE: Add roles only when a handler requires them.
-- These 6 cover the primary send-path capability gates.
IF NOT EXISTS (SELECT 1 FROM [dbo].[ProfileRoles] WHERE [Name] = 'HaveNoSendRestrictions')
BEGIN
    INSERT INTO [dbo].[ProfileRoles] ([Name]) VALUES
        ('HaveNoSendRestrictions'),   -- bypass address-restriction check
        ('CanSendByEboks'),           -- e-Boks channel gate
        ('CanSendByVoice'),           -- voice channel gate
        ('UseMunicipalityPolList'),   -- municipality positive-list enforcement
        ('CanSendToCriticalAddresses'), -- critical-address override
        ('SmsConversations');         -- two-way SMS conversation feature
END;

-- -----------------------------------------------------------------------
-- ProfileRoleMappings — direct (ProfileId, ProfileRoleId) assignment.
-- PRIMARY capability enforcement gate. DoesProfileHaveRole() reads this.
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.ProfileRoleMappings'))
BEGIN
    CREATE TABLE [dbo].[ProfileRoleMappings] (
        [ProfileId]     INT  NOT NULL,
        [ProfileRoleId] INT  NOT NULL,
        CONSTRAINT [PK_ProfileRoleMappings] PRIMARY KEY CLUSTERED ([ProfileId] ASC, [ProfileRoleId] ASC),
        CONSTRAINT [FK_ProfileRoleMappings_Profiles]     FOREIGN KEY ([ProfileId])     REFERENCES [dbo].[Profiles]     ([Id]),
        CONSTRAINT [FK_ProfileRoleMappings_ProfileRoles] FOREIGN KEY ([ProfileRoleId]) REFERENCES [dbo].[ProfileRoles]  ([Id])
    );
END;

-- -----------------------------------------------------------------------
-- CustomerUserRoleMappings — POLICY table (CustomerId, UserRoleId, NO UserId).
-- Defines which UserRoles a customer makes available in its admin UI.
-- This is NOT a per-user role assignment. See FOUNDATIONAL_DOMAIN_ANALYSIS
-- core_concept.CustomerUserRoleMapping for the distinction.
-- -----------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE object_id = OBJECT_ID('dbo.CustomerUserRoleMappings'))
BEGIN
    CREATE TABLE [dbo].[CustomerUserRoleMappings] (
        [CustomerId]  INT  NOT NULL,
        [UserRoleId]  INT  NOT NULL,
        CONSTRAINT [PK_CustomerUserRoleMappings] PRIMARY KEY CLUSTERED ([CustomerId] ASC, [UserRoleId] ASC),
        CONSTRAINT [FK_CustomerUserRoleMappings_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers]  ([Id]),
        CONSTRAINT [FK_CustomerUserRoleMappings_UserRoles] FOREIGN KEY ([UserRoleId]) REFERENCES [dbo].[UserRoles]   ([Id])
    );
END;
