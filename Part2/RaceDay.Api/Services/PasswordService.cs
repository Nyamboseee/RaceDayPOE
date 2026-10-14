using Microsoft.AspNetCore.Identity;

namespace RaceDay.Api.Services;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string hashedPassword, string providedPassword);
}

public class PasswordService : IPasswordService
{
    // The hasher adds a random salt and runs many rounds,
    // so the same password gives a different hash each time
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password)
    {
        return _hasher.HashPassword(new object(), password);
    }

    public bool Verify(string hashedPassword, string providedPassword)
    {
        try
        {
            var result = _hasher.VerifyHashedPassword(new object(), hashedPassword, providedPassword);
            return result != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // Not a real hash (e.g. the seed accounts), so nobody can log in with it
            return false;
        }
    }
}