namespace GreenAi.Api.SharedKernel.Localization;

public sealed class Country
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string TwoLetterIsoCode { get; init; } = "";
    public string ThreeLetterIsoCode { get; init; } = "";
    public short NumericIsoCode { get; init; }
    public int PhoneCode { get; init; }
    public int LanguageId { get; init; }
    public int DisplayOrder { get; init; }
}
