namespace GreenAi.Api.SharedKernel.Localization;

/// <summary>
/// Well-known Country IDs (dbo.Countries.Id).
/// These are NOT interchangeable with LanguageIds — use Countries.LanguageId FK to resolve.
/// </summary>
public static class CountryIds
{
    public const int Denmark       = 1;
    public const int Sweden        = 2;
    public const int UnitedKingdom = 3;
    public const int Finland       = 4;
    public const int Norway        = 5;
    public const int Germany       = 6;
}

/// <summary>
/// Well-known Language IDs (dbo.Languages.Id).
/// These are NOT interchangeable with CountryIds — a country points to a language via FK.
/// </summary>
public static class LanguageIds
{
    public const int Danish    = 1;
    public const int Swedish   = 2;
    public const int English   = 3;
    public const int Finnish   = 4;
    public const int Norwegian = 5;
    public const int German    = 6;
}

/// <summary>
/// Phone routing rules per country.
/// </summary>
public static class PhoneConstants
{
    public const int CodeDenmark       = 45;
    public const int CodeSweden        = 46;
    public const int CodeUnitedKingdom = 44;
    public const int CodeFinland       = 358;
    public const int CodeNorway        = 47;
    public const int CodeGermany       = 49;

    public static int GetPhoneCode(int countryId) => countryId switch
    {
        CountryIds.Denmark       => CodeDenmark,
        CountryIds.Sweden        => CodeSweden,
        CountryIds.UnitedKingdom => CodeUnitedKingdom,
        CountryIds.Finland       => CodeFinland,
        CountryIds.Norway        => CodeNorway,
        CountryIds.Germany       => CodeGermany,
        _                        => 0,
    };

    /// <summary>
    /// Returns (Min, Max) digit count for a phone number after the country code.
    /// </summary>
    public static (int Min, int Max) GetLengthRule(int countryId) => countryId switch
    {
        CountryIds.Denmark       => (8, 8),
        CountryIds.Norway        => (8, 8),
        CountryIds.Sweden        => (9, 15),
        CountryIds.Finland       => (6, 12),
        CountryIds.Germany       => (7, 12),
        CountryIds.UnitedKingdom => (8, 10),
        _                        => (6, 15),
    };
}
