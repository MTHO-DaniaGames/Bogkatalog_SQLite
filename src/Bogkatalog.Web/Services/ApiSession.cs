namespace Bogkatalog.Web.Services;
// Scoped: hver Blazor-forbindelse har sin egen session på Web-serveren.
// Der bruges hverken localStorage eller en fælles, statisk tokenvariabel.
public sealed class ApiSession
{
    public string? AccessToken { get; private set; }
    public string? Username { get; private set; }
    public string? Role { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsLoggedIn => AccessToken is not null;
    public void Set(string token, string username, string role, int expiresIn)
    { AccessToken = token; Username = username; Role = role; ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn); }
    public void Clear() { AccessToken = Username = Role = null; ExpiresAt = default; }
}
