using System.Security.Cryptography;
using System.Text;
using MySvelteApp.Server.Application.Common.Interfaces;

namespace MySvelteApp.Server.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    public (string Hash, string Salt) HashPassword(string password)
    {
        using var hmac = new HMACSHA512();
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return (Convert.ToBase64String(hash), Convert.ToBase64String(hmac.Key));
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        try
        {
            var saltBytes = Convert.FromBase64String(salt);
            using var hmac = new HMACSHA512(saltBytes);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            var storedHash = Convert.FromBase64String(hash);
            return computedHash.SequenceEqual(storedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
