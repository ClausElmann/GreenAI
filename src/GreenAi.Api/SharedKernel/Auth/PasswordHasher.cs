using System.Security.Cryptography;
using System.Text;

namespace GreenAi.Api.SharedKernel.Auth;

/// <summary>
/// Password hashing using PBKDF2/SHA-512 — one-way, per-user salt.
/// Upgrade fra sms-service's SHA256 (ingen KDF).
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 32;
    private const int HashSize = 64;
    private const int Iterations = 100_000;

    public static (string Hash, string Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Pbkdf2(password, salt);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool Verify(string password, string storedHash, string storedSalt)
    {
        var salt = Convert.FromBase64String(storedSalt);
        var hash = Pbkdf2(password, salt);
        return CryptographicOperations.FixedTimeEquals(
            hash,
            Convert.FromBase64String(storedHash));
    }

    private static byte[] Pbkdf2(string password, byte[] salt)
        => Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA512,
            HashSize);
}
