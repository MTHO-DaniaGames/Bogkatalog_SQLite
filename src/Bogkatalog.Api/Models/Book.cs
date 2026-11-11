namespace Bogkatalog.Api.Models;

// En record samler bogens værdier i et objekt, som vi ikke ændrer efter oprettelse.
public sealed record Book(int Id, string Title, string Author, int Pages);
