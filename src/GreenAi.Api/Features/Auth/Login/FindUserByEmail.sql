-- FindUserByEmail.sql
-- PRE-AUTH identity query: resolves global user identity only.
-- Rule: pre_auth_sql_tenant_exception — must NOT contain CustomerId, Profiles join,
-- or any tenant/profile resolution. Returns credentials and lock state only.

SELECT
    u.[Id],
    u.[Email],
    u.[PasswordHash],
    u.[PasswordSalt],
    u.[FailedLoginCount],
    u.[IsLockedOut]
FROM [dbo].[Users] u
WHERE u.[Email]    = @Email
  AND u.[IsActive] = 1;
