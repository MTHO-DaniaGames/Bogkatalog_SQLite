using Bogkatalog.Web.Components;
using Bogkatalog.Web.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
// API-clienten opretter en named HttpClient til hvert kald.
builder.Services.AddHttpClient("Books", http =>
{
    var address = builder.Configuration["BooksApi:BaseUrl"]
        ?? throw new InvalidOperationException("BooksApi:BaseUrl mangler.");
    http.BaseAddress = new Uri(address);
    var seconds = builder.Configuration.GetValue<int?>("BooksApi:TimeoutSeconds") ?? 3;
    if (seconds < 1 || seconds > 60) throw new InvalidOperationException("Timeout skal være 1 til 60 sekunder.");
    http.Timeout = TimeSpan.FromSeconds(seconds);
});
builder.Services.AddScoped<ApiSession>();
builder.Services.AddScoped<BookApiClient>();
var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
