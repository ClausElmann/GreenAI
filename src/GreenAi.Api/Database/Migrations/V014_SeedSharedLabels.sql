-- V014 SeedSharedLabels
-- Seeds shared.* localization labels in Danish (LanguageId=1) and English (LanguageId=3).
-- These are universal UI labels reused across all features.
-- Depends: V011 (Labels table), V010 (Languages seed with Id=1=DA, Id=3=EN)
-- Idempotent: skips rows that already exist.

SET NOCOUNT ON;

INSERT INTO [dbo].[Labels] ([ResourceName], [ResourceValue], [LanguageId])
SELECT v.[ResourceName], v.[ResourceValue], v.[LanguageId]
FROM (VALUES
    -- Buttons
    ('shared.SaveButton',              'Gem',                                   1),
    ('shared.SaveButton',              'Save',                                  3),
    ('shared.CancelButton',            'Annuller',                              1),
    ('shared.CancelButton',            'Cancel',                                3),
    ('shared.DeleteButton',            'Slet',                                  1),
    ('shared.DeleteButton',            'Delete',                                3),
    ('shared.EditButton',              'Rediger',                               1),
    ('shared.EditButton',              'Edit',                                  3),
    ('shared.CloseButton',             'Luk',                                   1),
    ('shared.CloseButton',             'Close',                                 3),
    ('shared.ClearButton',             'Ryd',                                   1),
    ('shared.ClearButton',             'Clear',                                 3),
    ('shared.ExportButton',            'Eksport',                               1),
    ('shared.ExportButton',            'Export',                                3),
    ('shared.RefreshButton',           'Opdater',                               1),
    ('shared.RefreshButton',           'Refresh',                               3),
    ('shared.CreateEntityButton',      'Opret ny {0}',                          1),
    ('shared.CreateEntityButton',      'Create new {0}',                        3),
    -- Inputs
    ('shared.SearchPlaceholder',       'Søg...',                                1),
    ('shared.SearchPlaceholder',       'Search...',                             3),
    -- Labels
    ('shared.StatusLabel',             'Status',                                1),
    ('shared.StatusLabel',             'Status',                                3),
    ('shared.ColumnName',              'Navn',                                  1),
    ('shared.ColumnName',              'Name',                                  3),
    ('shared.ColumnStatus',            'Status',                                1),
    ('shared.ColumnStatus',            'Status',                                3),
    -- Messages
    ('shared.SaveSuccess',             'Gemt!',                                 1),
    ('shared.SaveSuccess',             'Saved!',                                3),
    ('shared.DeleteConfirmFormat',     'Er du sikker på at slette {0}?',        1),
    ('shared.DeleteConfirmFormat',     'Are you sure you want to delete {0}?',  3)
) AS v ([ResourceName], [ResourceValue], [LanguageId])
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Labels] l
    WHERE l.[ResourceName] = v.[ResourceName]
      AND l.[LanguageId]   = v.[LanguageId]
);
