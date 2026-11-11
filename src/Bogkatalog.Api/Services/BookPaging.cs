using Bogkatalog.Api.Models;
namespace Bogkatalog.Api.Services;

public static class BookPaging
{
    public static IResult GetPage(IBookRepository store, int page, int pageSize, string? title)
    {
        var errors = new Dictionary<string,string[]>();
        if (page < 1) errors["page"] = ["page skal være mindst 1."];
        if (pageSize < 1 || pageSize > 20) errors["pageSize"] = ["pageSize skal være fra 1 til 20."];
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        var books = store.GetAll().Where(b => string.IsNullOrWhiteSpace(title)
            || b.Title.Contains(title,StringComparison.OrdinalIgnoreCase)).OrderBy(b=>b.Id).ToArray();
        var items = Slice(books,page,pageSize);
        string Url(int number) => $"/api/books?page={number}&pageSize={pageSize}"
            + (string.IsNullOrWhiteSpace(title) ? "" : "&title="+Uri.EscapeDataString(title));
        var links = new PageLinks(Url(page), page > 1 ? Url(page-1) : null,
            (long)page * pageSize < books.Length ? Url(page+1) : null);
        return Results.Ok(new BookPage(items,page,pageSize,books.Length,links));
    }
    public static Book[] Slice(Book[] books, int page, int pageSize)
    {
        long offset = (long)(page - 1) * pageSize;
        if (offset >= books.Length) return [];
        // Først springes tidligere sider over, derefter vælges sidens bøger.
        return books.Skip((int)offset).Take(pageSize).ToArray();
    }
}
public sealed record BookPage(Book[] Items,int Page,int PageSize,int TotalCount,PageLinks Links);
public sealed record PageLinks(string Self,string? Previous,string? Next);
