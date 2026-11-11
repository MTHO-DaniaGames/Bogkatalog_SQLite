namespace Bogkatalog.Api.Contracts;

// Request DTO. Id vælges af API'et og er derfor ikke med her.
public class BookInput
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public int Pages { get; set; }
}
