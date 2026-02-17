using System.Security.Cryptography;
using System.Text;

namespace CCAT.Mvp1.Api.Security;

public static class PasswordHasher
{
    public static (byte[] Hash, byte[] Salt) HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password es obligatorio.");

        byte[] salt = RandomNumberGenerator.GetBytes(32); // 32 bytes = VARBINARY(32)
        byte[] hash = ComputeSha512Hash(password, salt);   // 64 bytes = VARBINARY(64)
        return (hash, salt);
    }

    public static bool VerifyPassword(string password, byte[] salt, byte[] expectedHash)
    {
        var computed = ComputeSha512Hash(password, salt);
        return CryptographicOperations.FixedTimeEquals(computed, expectedHash);
    }

    private static byte[] ComputeSha512Hash(string password, byte[] salt)
    {
        using var sha = SHA512.Create();
        byte[] pwdBytes = Encoding.UTF8.GetBytes(password);

        // hash = SHA512(salt + password)
        byte[] input = new byte[salt.Length + pwdBytes.Length];
        Buffer.BlockCopy(salt, 0, input, 0, salt.Length);
        Buffer.BlockCopy(pwdBytes, 0, input, salt.Length, pwdBytes.Length);

        return sha.ComputeHash(input);
    }
}
