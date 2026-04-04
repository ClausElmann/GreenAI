-- V023: Renumber ApplicationSettingTypeId to start at 1 (green-ai native).
-- Old values were inadvertently copied from sms-service (160, 310-316, 320-321).
-- Green-ai uses: Logging=1, SMTP=10-16, PasswordReset=20-21.

UPDATE [dbo].[ApplicationSettings]
SET    [ApplicationSettingTypeId] = 1,
       [Name]                     = 'RequestLogLevel'
WHERE  [ApplicationSettingTypeId] = 160;
