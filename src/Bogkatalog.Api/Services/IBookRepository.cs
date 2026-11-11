using Bogkatalog.Api.Contracts;
using Bogkatalog.Api.Models;

namespace Bogkatalog.Api.Services;

public interface IBookRepository
{
    Book[] GetAll();
    Book? GetById(int id);
    Book Add(BookInput input);
    Book? Update(int id, BookInput input);
    bool Delete(int id);
}
