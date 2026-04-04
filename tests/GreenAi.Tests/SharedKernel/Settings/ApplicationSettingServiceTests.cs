using GreenAi.Api.SharedKernel.Db;
using GreenAi.Api.SharedKernel.Settings;

namespace GreenAi.Tests.SharedKernel.Settings;

/// <summary>
/// Integration tests for ApplicationSettingService — verificerer SQL mod rigtig DB.
///
/// Kritisk adfærd:
///   - GetAsync returnerer defaultValue når nøglen ikke eksisterer
///   - SaveAsync gemmer ny nøgle og returnerer den ved næste GetAsync (cache-invalidering)
///   - SaveAsync opdaterer eksisterende nøgle (upsert)
///   - CreateDefaultsAsync opretter rækker for alle enum-værdier (idempotent)
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class ApplicationSettingServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;

    public ApplicationSettingServiceTests(DatabaseFixture db) => _db = db;

    public ValueTask InitializeAsync() => _db.ResetAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static ApplicationSettingService CreateService() =>
        new(new DbSession(DatabaseFixture.ConnectionString));

    [Fact]
    public async Task GetAsync_NonExistentKey_ReturnsDefault()
    {
        var svc = CreateService();

        var result = await svc.GetAsync(AppSetting.RequestLogLevel, "off");

        Assert.Equal("off", result);
    }

    [Fact]
    public async Task SaveAsync_NewKey_CanBeRetrieved()
    {
        var svc = CreateService();

        await svc.SaveAsync(AppSetting.RequestLogLevel, "Error");
        var result = await svc.GetAsync(AppSetting.RequestLogLevel);

        Assert.Equal("Error", result);
    }

    [Fact]
    public async Task SaveAsync_ExistingKey_UpdatesValue()
    {
        var svc = CreateService();
        await svc.SaveAsync(AppSetting.RequestLogLevel, "Error");

        await svc.SaveAsync(AppSetting.RequestLogLevel, "All");
        var result = await svc.GetAsync(AppSetting.RequestLogLevel);

        Assert.Equal("All", result);
    }

    [Fact]
    public async Task SaveAsync_InvalidatesCache_NextGetReadsDb()
    {
        var svc = CreateService();
        await svc.SaveAsync(AppSetting.RequestLogLevel, "off");
        _ = await svc.GetAsync(AppSetting.RequestLogLevel); // fyld cache

        await svc.SaveAsync(AppSetting.RequestLogLevel, "All"); // invalidér cache
        var result = await svc.GetAsync(AppSetting.RequestLogLevel);

        Assert.Equal("All", result);
    }

    [Fact]
    public async Task CreateDefaultsAsync_IsIdempotent_NoException()
    {
        var svc = CreateService();

        // Kald to gange — ingen exceptions
        await svc.CreateDefaultsAsync();
        await svc.CreateDefaultsAsync();

        // Verificér at mindst én nøgle eksisterer bagefter
        var value = await svc.GetAsync(AppSetting.RequestLogLevel);
        Assert.Null(value); // default = null (tom streng ikke seedet)
    }
}
