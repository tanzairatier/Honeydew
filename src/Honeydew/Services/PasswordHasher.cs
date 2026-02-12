using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Honeydew.Services;

public static class PasswordHasher
{
    private const int SaltSize = 128 / 8;
    private const int IterationCount = 100_000;
    private const int HashSize = 256 / 8;

    public static (string Hash, string Salt) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = KeyDerivation.Pbkdf2(
            password,
            salt,
            KeyDerivationPrf.HMACSHA256,
            IterationCount,
            HashSize);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool VerifyPassword(string password, string storedHash, string? storedSalt)
    {
        if (string.IsNullOrEmpty(storedSalt))
        {
            return false;
        }
        var salt = Convert.FromBase64String(storedSalt);
        var hash = KeyDerivation.Pbkdf2(
            password,
            salt,
            KeyDerivationPrf.HMACSHA256,
            IterationCount,
            HashSize);
        return CryptographicOperations.FixedTimeEquals(hash, Convert.FromBase64String(storedHash));
    }
}
