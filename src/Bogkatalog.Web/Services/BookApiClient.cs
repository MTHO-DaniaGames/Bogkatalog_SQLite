using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bogkatalog.Web.Models;
namespace Bogkatalog.Web.Services;

public class BookApiClient(IHttpClientFactory factory, ApiSession session)
{
    private HttpClient CreateClient()
    {
        var http = factory.CreateClient("Books");
        if (session.AccessToken is { } token)
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return http;
    }
    public async Task LoginAsync(string username, string password)
    {
        session.Clear();
        using var http = factory.CreateClient("Books");
        using var response = await http.PostAsJsonAsync("auth/login", new { username, password });
        await CheckAsync(response);
        var login = await response.Content.ReadFromJsonAsync<TokenResponse>()
            ?? throw new JsonException("Login-svaret mangler.");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        using var me = await http.GetAsync("auth/me");
        await CheckAsync(me);
        var profile = await me.Content.ReadFromJsonAsync<Profile>() ?? throw new JsonException("Profilen mangler.");
        session.Set(login.AccessToken, profile.Username, profile.Role, login.ExpiresIn);
    }
    public async Task<BookPage> GetPageAsync(int page)
    {
        using var http = CreateClient();
        using var response = await http.GetAsync($"api/books?page={page}&pageSize=2");
        await CheckAsync(response);
        return await response.Content.ReadFromJsonAsync<BookPage>() ?? throw new JsonException("Boglisten mangler.");
    }
    public async Task<Book> AddAsync(BookInput input)
    {
        using var http = CreateClient();
        using var response = await http.PostAsJsonAsync("api/books", input);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationResponse>();
            throw new BookValidationException(problem?.Errors ?? new());
        }
        await CheckAsync(response);
        return await response.Content.ReadFromJsonAsync<Book>() ?? throw new JsonException("Bogen mangler.");
    }
    private static async Task CheckAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        // Statuskoden er det afgørende. Fejl-body er ekstra information.
        string message = (int)response.StatusCode switch
        {
            401 => "Login mangler eller er udløbet. Log ind igen.",
            403 => "I er logget ind, men denne konto har ikke adgang til handlingen.",
            429 => "For mange loginforsøg. Vent et minut og prøv igen.",
            _ => "API-kaldet mislykkedes."
        };
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new BookApiException(401, message);
        throw new BookApiException((int)response.StatusCode, message);
    }
}
public sealed record TokenResponse(string AccessToken, int ExpiresIn);
public sealed record Profile(string Username, string Role);
public sealed record BookPage(List<Book> Items, int Page, int PageSize, int TotalCount, PageLinks Links);
public sealed record PageLinks(string Self, string? Previous, string? Next);
public sealed class ValidationResponse { public Dictionary<string, string[]> Errors { get; set; } = new(); }
public sealed class BookApiException(int status, string title) : Exception(title) { public int Status { get; } = status; }
public sealed class BookValidationException(Dictionary<string, string[]> errors) : Exception("Ugyldigt input")
{ public Dictionary<string, string[]> Errors { get; } = errors; }
