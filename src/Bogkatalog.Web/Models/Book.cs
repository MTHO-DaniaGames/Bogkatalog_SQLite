using System.ComponentModel.DataAnnotations;
namespace Bogkatalog.Web.Models;
public class Book
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Skriv en titel.")]
    public string Title { get; set; } = "";
    [Required(ErrorMessage = "Skriv en forfatter.")]
    public string Author { get; set; } = "";
    [Range(1, 5000, ErrorMessage = "Antal sider skal være mellem 1 og 5000.")]
    public int Pages { get; set; } = 100;
}
