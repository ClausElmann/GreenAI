MERGE ApplicationSettings AS target
USING (SELECT @TypeId AS ApplicationSettingTypeId) AS source
    ON target.ApplicationSettingTypeId = source.ApplicationSettingTypeId
WHEN MATCHED THEN
    UPDATE SET
        Value     = @Value,
        Name      = @Name,
        UpdatedAt = SYSDATETIMEOFFSET()
WHEN NOT MATCHED THEN
    INSERT (ApplicationSettingTypeId, Name, Value, UpdatedAt)
    VALUES (@TypeId, @Name, @Value, SYSDATETIMEOFFSET());
