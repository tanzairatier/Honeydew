using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Honeydew.Services;

public static class ClientSecretHasher
{
    private const int IterationCount = 100_000;
    private const int HashSize = 256 / 8;

    /// <summary>Hash client secret using clientId as salt so we don't store a separate salt.</summary>
    public static string Hash(string clientSecret, string clientId)
    {
        var salt = System.Text.Encoding.UTF8.GetBytes(clientId);
        var hash = KeyDerivation.Pbkdf2(
            clientSecret,
            salt,
            KeyDerivationPrf.HMACSHA256,
            IterationCount,
            HashSize);
        return Convert.ToBase64String(hash);
    }

    public static bool Verify(string clientSecret, string clientId, string storedHash)
    {
        var computed = Hash(clientSecret, clientId);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(computed),
            Convert.FromBase64String(storedHash));
    }
}
