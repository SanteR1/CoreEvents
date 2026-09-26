using System.Security.Cryptography;
using System.Text;
using Users.Application.Interfaces.Identity;
using Konscious.Security.Cryptography;

namespace Users.Infrastructure.Identity;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "S101:Types should be named in PascalCase", Justification = "Argon2id is the standard cryptographic algorithm name")]
public class Argon2idPasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;       // 128 бит соли
    private const int HashSize = 32;       // 256 бит хеша
    private const int Iterations = 3;      // Рекомендация OWASP
    private const int MemorySize = 65536;  // 64 МБ
    private const int DegreeOfParallelism = 1;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = GenerateHash(password, salt);

        return $"$argon2id$v=19$m={MemorySize},t={Iterations},p={DegreeOfParallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);


        // Обратная совместимость со старым SHA-256 хешами
        if (!hash.StartsWith("$argon2id$", StringComparison.Ordinal))
        {
            return VerifyLegacySha256(password, hash);
        }

        var parts = hash.Split('$');
        if (parts.Length != 6) return false;

        byte[] salt = Convert.FromBase64String(parts[4]);
        byte[] expectedHash = Convert.FromBase64String(parts[5]);

        byte[] actualHash = GenerateHash(password, salt);

        if (CryptographicOperations.FixedTimeEquals(actualHash, expectedHash)) return true;

        return false;
    }

    public bool NeedsRehash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) return true;

        // Если это старый SHA-256 хеш — нужен рехеш
        if (!hash.StartsWith("$argon2id$", StringComparison.Ordinal)) return true;

        // Проверяем, соответствуют ли параметры хеша актуальным настройкам сложности
        return !hash.Contains($"m={MemorySize},t={Iterations},p={DegreeOfParallelism}", StringComparison.Ordinal);
    }

    private static byte[] GenerateHash(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt, DegreeOfParallelism = DegreeOfParallelism, Iterations = Iterations, MemorySize = MemorySize
        };

        return argon2.GetBytes(HashSize);
    }

    private static bool VerifyLegacySha256(string password, string legacyHexHash)
    {
        var inputHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));

        var userHashBytes = Convert.FromHexString(legacyHexHash);
        if (CryptographicOperations.FixedTimeEquals(inputHashBytes, userHashBytes)) return true;

        return false;
    }
}
