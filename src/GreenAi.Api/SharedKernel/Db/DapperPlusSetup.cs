using Z.Dapper.Plus;

namespace GreenAi.Api.SharedKernel.Db;

public static class DapperPlusSetup
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        DapperPlusManager.AddLicense("2181;701-BlueIdea", "63c00228-3908-20d4-34ef-36fbc21070b5");

        if (!DapperPlusManager.ValidateLicense(out var error, DapperProviderType.SqlServer))
            throw new InvalidOperationException($"Dapper.Plus license invalid: {error}");
    }
}
