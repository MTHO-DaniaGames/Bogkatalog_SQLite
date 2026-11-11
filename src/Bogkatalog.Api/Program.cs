using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.RateLimiting;
using Bogkatalog.Api.Contracts;
using Bogkatalog.Api.Services;
using Bogkatalog.Api.Validation;
using Bogkatalog.Api.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IBookRepository, SqliteBookRepository>();
builder.Services.AddSingleton<DemoUsers>();
builder.Services.AddProblemDetails();
builder.Services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
    .AddBearerToken(options =>
    {
        options.BearerTokenExpiration = TimeSpan.FromMinutes(15);
        options.RefreshTokenExpiration = TimeSpan.FromMinutes(15);
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReadBooks", policy => policy.RequireAuthenticatedUser().RequireRole("Reader", "Editor"));
    options.AddPolicy("WriteBooks", policy => policy.RequireAuthenticatedUser().RequireRole("Editor"));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    await next(context);
    // Log kun metode, path og status. Ingen adgangskoder, tokens eller body.
    app.Logger.LogInformation("{Method} {Path} HTTP {Status}",
        context.Request.Method, context.Request.Path, context.Response.StatusCode);
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
_ = app.Services.GetRequiredService<IBookRepository>();
_ = app.Services.GetRequiredService<DemoUsers>();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Bogkatalog.Api" }));
app.MapPost("/auth/login", IResult (LoginInput input, DemoUsers users) =>
{
    var principal = users.Authenticate(input.Username, input.Password);
    return principal is null
        ? Results.Problem(statusCode: 401, title: "Brugernavn eller adgangskode er forkert.")
        : Results.SignIn(principal, authenticationScheme: BearerTokenDefaults.AuthenticationScheme);
}).RequireRateLimiting("login");
app.MapGet("/auth/me", (ClaimsPrincipal user) => Results.Ok(new
{
    username = user.Identity!.Name,
    role = user.FindFirstValue(ClaimTypes.Role)
})).RequireAuthorization();

// Alle bog-endpoints kræver som minimum en læser eller redaktør.
var books = app.MapGroup("/api/books").RequireAuthorization("ReadBooks");
books.MapGet("", IResult (int? page, int? pageSize, string? title, IBookRepository store) =>
    BookPaging.GetPage(store, page ?? 1, pageSize ?? 2, title));
books.MapGet("/{id:int}", IResult (int id, IBookRepository store) =>
    store.GetById(id) is { } book ? Results.Ok(book)
    : Results.Problem(statusCode: 404, title: "Bogen findes ikke."));
books.MapPost("", IResult (BookInput input, IBookRepository store) =>
{
    var errors = BookRules.Validate(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var book = store.Add(input);
    return Results.Created($"/api/books/{book.Id}", book);
}).RequireAuthorization("WriteBooks");
books.MapPut("/{id:int}", IResult (int id, BookInput input, IBookRepository store) =>
{
    var errors = BookRules.Validate(input);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    return store.Update(id, input) is { } book ? Results.Ok(book)
        : Results.Problem(statusCode: 404, title: "Bogen findes ikke.");
}).RequireAuthorization("WriteBooks");
books.MapDelete("/{id:int}", IResult (int id, IBookRepository store) =>
    store.Delete(id) ? Results.NoContent()
    : Results.Problem(statusCode: 404, title: "Bogen findes ikke."))
    .RequireAuthorization("WriteBooks");
app.Run();
