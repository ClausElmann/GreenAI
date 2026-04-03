-- CheckEmailAvailable.sql
-- Returns the count of active users with the given email, excluding the current user.
-- A result of 0 means the email is available.
SELECT COUNT(*)
FROM   Users
WHERE  Email = @Email
AND    Id    <> @ExcludeUserId;
