-- VNM Reference Data Seed Script
-- Roles and their permission claims.
-- Run once during initial DB setup / deployment. Fully idempotent.
-- Users are NOT seeded here — they are auto-provisioned on first login via the BFF.
-- After seeding, assign users to roles via the admin UI or POST /api/permissions/roles/{roleId}/users/{userId}.
-- ============================================================

DECLARE @AdminRoleId   NVARCHAR(450)
DECLARE @ContributorRoleId NVARCHAR(450)

-- ── Roles ─────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [Name] = 'admin')
BEGIN
    SET @AdminRoleId = NEWID()
    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name]) VALUES (@AdminRoleId, 'admin')
    PRINT 'Created role: admin'
END
ELSE
BEGIN
    SELECT @AdminRoleId = [Id] FROM [dbo].[AspNetRoles] WHERE [Name] = 'admin'
    PRINT 'Role already exists: admin'
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[AspNetRoles] WHERE [Name] = 'contributors')
BEGIN
    SET @ContributorRoleId = NEWID()
    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name]) VALUES (@ContributorRoleId, 'contributors')
    PRINT 'Created role: contributors'
END
ELSE
BEGIN
    SELECT @ContributorRoleId = [Id] FROM [dbo].[AspNetRoles] WHERE [Name] = 'contributors'
    PRINT 'Role already exists: contributors'
END

-- ── admin role claims ──────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[AspNetRoleClaims]
    WHERE [RoleId] = @AdminRoleId AND [ClaimType] = 'permission' AND [ClaimValue] = 'dashboard:read'
)
BEGIN
    INSERT INTO [dbo].[AspNetRoleClaims] ([RoleId], [ClaimType], [ClaimValue])
    VALUES (@AdminRoleId, 'permission', 'dashboard:read')
    PRINT 'Granted admin: dashboard:read'
END

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[AspNetRoleClaims]
    WHERE [RoleId] = @AdminRoleId AND [ClaimType] = 'permission' AND [ClaimValue] = 'dashboard:retry'
)
BEGIN
    INSERT INTO [dbo].[AspNetRoleClaims] ([RoleId], [ClaimType], [ClaimValue])
    VALUES (@AdminRoleId, 'permission', 'dashboard:retry')
    PRINT 'Granted admin: dashboard:retry'
END

-- ── contributors role claims ───────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[AspNetRoleClaims]
    WHERE [RoleId] = @ContributorRoleId AND [ClaimType] = 'permission' AND [ClaimValue] = 'dashboard:read'
)
BEGIN
    INSERT INTO [dbo].[AspNetRoleClaims] ([RoleId], [ClaimType], [ClaimValue])
    VALUES (@ContributorRoleId, 'permission', 'dashboard:read')
    PRINT 'Granted contributors: dashboard:read'
END
