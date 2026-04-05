namespace GreenAi.E2E;

/// <summary>
/// Cached auth tokens — fetched ONCE per test process via the auth API, then
/// reused by every test (both E2ETestBase and VisualTestBase).
/// Avoids one HTTP round-trip per test (was ~150 ms each).
/// Tokens are long-lived (default JWT lifetime in dev = 60 min) so reuse is safe
/// within a single test run.
/// </summary>
internal sealed record LoginTokens(string AccessToken, string RefreshToken, string ExpiresAt);

internal static class SharedAuth
{
    private const string BaseUrl = "http://localhost:5057";

    // One lazy per credential pair — currently only the primary dev account is used.
    private static readonly Lazy<Task<LoginTokens>> _primary =
        new(static () => AcquireAsync("claus.elmann@gmail.com", "Flipper12#"),
            LazyThreadSafetyMode.ExecutionAndPublication);

    public static Task<LoginTokens> PrimaryAsync() => _primary.Value;

    private static async Task<LoginTokens> AcquireAsync(string email, string password)
    {
        using var http = new HttpClient();
        var body = System.Text.Json.JsonSerializer.Serialize(new { email, password });
        var response = await http.PostAsync(
            $"{BaseUrl}/api/auth/login",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
            throw new Exception(
                $"SharedAuth: Login API returned {response.StatusCode} for {email}. " +
                "Check credentials and that the backend is running on http://localhost:5057.");

        var json = System.Text.Json.JsonDocument
            .Parse(await response.Content.ReadAsStringAsync()).RootElement;

        return new LoginTokens(
            json.GetProperty("accessToken").GetString()!,
            json.GetProperty("refreshToken").GetString()!,
            json.GetProperty("expiresAt").GetString()!);
    }
}
