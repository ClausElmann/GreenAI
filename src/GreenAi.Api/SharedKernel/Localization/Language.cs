namespace GreenAi.Api.SharedKernel.Localization;

public sealed class Language
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string LanguageCulture { get; init; } = "";  // 'da-DK', 'sv-SE', etc.
    public string UniqueSeoCode { get; init; } = "";    // 'da', 'sv', etc.
    public bool Published { get; init; }
    public int DisplayOrder { get; init; }
}
