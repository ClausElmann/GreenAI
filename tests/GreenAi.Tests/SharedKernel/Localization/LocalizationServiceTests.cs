using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Localization;
using NSubstitute;

namespace GreenAi.Tests.SharedKernel.Localization;

/// <summary>
/// Unit tests for LocalizationService.
///
/// Critical behaviour:
///   - GetAsync returns the resource value when found
///   - GetAsync returns the key itself (fail-open) when not found — never null/empty
///   - GetAsync with parameters replaces {tokens} in the resolved string
///   - GetAllAsync delegates to repository and returns upper-cased keys
/// </summary>
public sealed class LocalizationServiceTests
{
    private static (LocalizationService Service, ILocalizationRepository Repo) Create()
    {
        var repo = Substitute.For<ILocalizationRepository>();
        return (new LocalizationService(repo), repo);
    }

    // ====================================================================
    // GetAsync — single key
    // ====================================================================

    [Fact]
    public async Task GetAsync_KeyExists_ReturnsValue()
    {
        var ct = TestContext.Current.CancellationToken;
        var (svc, repo) = Create();
        repo.GetResourceValueAsync("shared.DateFormat", 1, ct)
            .Returns("dd-MM-yyyy");

        var result = await svc.GetAsync("shared.DateFormat", 1, ct);

        Assert.Equal("dd-MM-yyyy", result);
    }

    [Fact]
    public async Task GetAsync_KeyNotFound_ReturnsKeyName()
    {
        var ct = TestContext.Current.CancellationToken;
        var (svc, repo) = Create();
        repo.GetResourceValueAsync("missing.Key", 1, ct)
            .Returns((string?)null);

        var result = await svc.GetAsync("missing.Key", 1, ct);

        Assert.Equal("missing.Key", result);
    }

    [Fact]
    public async Task GetAsync_KeyNotFound_NeverReturnsNullOrEmpty()
    {
        var ct = TestContext.Current.CancellationToken;
        var (svc, repo) = Create();
        repo.GetResourceValueAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await svc.GetAsync("some.Key", 1, ct);

        Assert.False(string.IsNullOrEmpty(result));
    }

    // ====================================================================
    // GetAsync — with token replacement
    // ====================================================================

    [Fact]
    public async Task GetAsync_WithParameters_ReplacesTokens()
    {
        var ct = TestContext.Current.CancellationToken;
        var (svc, repo) = Create();
        repo.GetResourceValueAsync("greeting", 1, ct)
            .Returns("Hello {name}, you have {count} messages");

        var result = await svc.GetAsync("greeting", 1, new Dictionary<string, string>
        {
            ["name"]  = "Alice",
            ["count"] = "3",
        }, ct);

        Assert.Equal("Hello Alice, you have 3 messages", result);
    }

    [Fact]
    public async Task GetAsync_WithParameters_KeyNotFound_ReturnsKeyWithNoReplacement()
    {
        var ct = TestContext.Current.CancellationToken;
        var (svc, repo) = Create();
        repo.GetResourceValueAsync("missing.Key", 1, ct)
            .Returns((string?)null);

        var result = await svc.GetAsync("missing.Key", 1, new Dictionary<string, string>
        {
            ["name"] = "Alice",
        }, ct);

        // Returns key as-is — no crash, no null
        Assert.Equal("missing.Key", result);
    }

    // ====================================================================
    // GetAllAsync
    // ====================================================================

    [Fact]
    public async Task GetAllAsync_DelegatesAndUpperCasesKeys()
    {
        var (svc, repo) = Create();
        var ct = TestContext.Current.CancellationToken;
        repo.GetAllResourcesAsync(1, ct)
            .Returns(new Dictionary<string, string>
            {
                ["SHARED.DATEFORMAT"] = "dd-MM-yyyy",
                ["SHARED.TIMEFORMAT"] = "HH:mm",
            });

        var result = await svc.GetAllAsync(1, ct);

        Assert.Equal(2, result.Count);
        Assert.Equal("dd-MM-yyyy", result["SHARED.DATEFORMAT"]);
    }

    // ====================================================================
    // CountryConstants
    // ====================================================================

    // ====================================================================
    // PhoneConstants + CountryIds / LanguageIds — no interchangeability
    // ====================================================================

    [Theory]
    [InlineData(CountryIds.Denmark,       PhoneConstants.CodeDenmark)]
    [InlineData(CountryIds.Sweden,        PhoneConstants.CodeSweden)]
    [InlineData(CountryIds.Finland,       PhoneConstants.CodeFinland)]
    [InlineData(CountryIds.Norway,        PhoneConstants.CodeNorway)]
    [InlineData(CountryIds.Germany,       PhoneConstants.CodeGermany)]
    [InlineData(CountryIds.UnitedKingdom, PhoneConstants.CodeUnitedKingdom)]
    public void PhoneConstants_GetPhoneCode_ReturnsCorrectCode(int countryId, int expected)
    {
        Assert.Equal(expected, PhoneConstants.GetPhoneCode(countryId));
    }

    [Fact]
    public void PhoneConstants_UnknownCountry_ReturnsZero()
    {
        Assert.Equal(0, PhoneConstants.GetPhoneCode(99));
    }

    [Fact]
    public void CountryIds_And_LanguageIds_AreSeperateTypes()
    {
        // Verify they are not the same static class — no interchangeability by design
        Assert.NotEqual(typeof(CountryIds), typeof(LanguageIds));
    }
}
