using Bogkatalog.Api.Contracts;

namespace Bogkatalog.Api.Validation;

public static class BookRules
{
    public static Dictionary<string, string[]> Validate(BookInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.Title))
            errors[nameof(input.Title)] = ["Skriv en titel."];
        if (string.IsNullOrWhiteSpace(input.Author))
            errors[nameof(input.Author)] = ["Skriv en forfatter."];
        if (input.Pages < 1 || input.Pages > 5000)
            errors[nameof(input.Pages)] = ["Sidetal skal være mellem 1 og 5000."];
        return errors;
    }
}
