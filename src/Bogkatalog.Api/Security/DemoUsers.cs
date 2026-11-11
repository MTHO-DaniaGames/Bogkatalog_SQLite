using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
namespace Bogkatalog.Api.Security;

// To lokale øvelseskonti. Ingen registrering, brugerdatabase eller adgangskodeskift.
public sealed class DemoUsers
{
    private sealed record User(string Name, string Role);
    private readonly PasswordHasher<User> hasher = new();
    private readonly List<(User User, string Hash)> users = new();
    public DemoUsers(IConfiguration configuration)
    {
        Add("reader", "Reader", configuration["DemoAuth:ReaderPassword"]);
        Add("editor", "Editor", configuration["DemoAuth:EditorPassword"]);
    }
    private void Add(string name, string role, string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
            throw new InvalidOperationException("Kør først Prepare-HTTPS.ps1. Demo-adgangskoder mangler eller er for korte.");
        var user = new User(name, role);
        users.Add((user, hasher.HashPassword(user, password)));
    }
    public ClaimsPrincipal? Authenticate(string? name, string? password)
    {
        if (password is null || password.Length > 256) return null;
        // Samme antal hashkontroller, også hvis brugernavnet ikke findes.
        User? matched = null;
        foreach (var entry in users)
        {
            bool valid = hasher.VerifyHashedPassword(entry.User, entry.Hash, password)
                != PasswordVerificationResult.Failed;
            if (valid && entry.User.Name == name) matched = entry.User;
        }
        return matched is null ? null : new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, matched.Name), new Claim(ClaimTypes.Role, matched.Role) },
            BearerTokenDefaults.AuthenticationScheme));
    }
}
public sealed record LoginInput(string? Username, string? Password);
