using System.Security.Cryptography;
using System.Text;
using Users.Application.Interfaces.Identity;

namespace Users.Infrastructure.Identity;

public class Sha256PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public bool Verify(string password, string hash)
    {
        var inputHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        var userHashBytes = Convert.FromHexString(hash);

        return CryptographicOperations.FixedTimeEquals(inputHashBytes, userHashBytes);
    }
}
