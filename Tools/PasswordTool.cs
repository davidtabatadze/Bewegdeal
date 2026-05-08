using System.Security.Cryptography;
using System.Text;

namespace Bewegdeal.Tools
{
    /// <summary>
    /// Provides static methods for securely hashing and verifying passwords using PBKDF2 with SHA-256.
    /// </summary>
    /// <remarks>This class is intended for scenarios where password storage and verification are required. It
    /// uses a cryptographically secure random salt and a high iteration count to help protect against brute-force
    /// attacks. All methods are thread-safe and do not maintain any internal state.</remarks>
    public static class PasswordTool
    {
        private const int Iterations = 100_000;
        private const int KeyLength = 32;   // bytes → 256 bits
        private const int SaltLength = 32;   // bytes

        /// <summary>Hashes <paramref name="plainText"/> and returns (hash, salt) as Base64 strings.</summary>
        public static (string Hash, string Salt) HashPassword(string plainText)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(SaltLength);
            var salt = Convert.ToBase64String(saltBytes);
            return (Compute(plainText, saltBytes), salt);
        }

        /// <summary>Constant-time comparison — safe against timing attacks.</summary>
        public static bool Verify(string plainText, string storedHash, string storedSalt)
        {
            var saltBytes = Convert.FromBase64String(storedSalt);
            var computedHash = Compute(plainText, saltBytes);

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash),
                Encoding.UTF8.GetBytes(storedHash));
        }

        /// <summary>Makes actual computation.</summary>
        private static string Compute(string plainText, byte[] saltBytes)
        {
            var key = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(plainText),
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256,
                KeyLength);

            return Convert.ToBase64String(key);
        }
    }
}
