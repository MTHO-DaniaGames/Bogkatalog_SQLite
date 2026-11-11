using Microsoft.Data.Sqlite;
using Bogkatalog.Api.Contracts;
using Bogkatalog.Api.Models;

namespace Bogkatalog.Api.Services;

// Repositoryet samler SQL og omdannelsen fra rækker til Book-objekter.
// Hvert metodekald åbner sin egen forbindelse; ingen fælles åben forbindelse.
public sealed class SqliteBookRepository : IBookRepository
{
    private readonly string connectionString;

    public SqliteBookRepository(IConfiguration configuration, IHostEnvironment environment,
                                ILogger<SqliteBookRepository> logger)
    {
        string configuredPath = configuration["BookStorage:Path"] ?? "App_Data/books.db";
        string databasePath = Path.GetFullPath(configuredPath, environment.ContentRootPath);
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true,
            DefaultTimeout = 5
        }.ToString();
        Initialize(environment.ContentRootPath);
        logger.LogInformation("SQLite: {Path}. Databasen er klar.", databasePath);
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(connectionString);
        try { connection.Open(); return connection; }
        catch { connection.Dispose(); throw; }
    }

    private void Initialize(string contentRoot)
    {
        using var connection = OpenConnection();
        // WAL gør, at læsning kan fortsætte, mens en anden forbindelse skriver.
        using var journal = connection.CreateCommand();
        journal.CommandText = "PRAGMA journal_mode = WAL;";
        journal.ExecuteScalar();
        // Skema og startdata oprettes samlet eller slet ikke.
        using var transaction = connection.BeginTransaction();
        using var check = connection.CreateCommand();
        check.Transaction = transaction;
        check.CommandText = "SELECT COUNT(*) FROM sqlite_schema WHERE type = 'table' AND name = 'Books';";
        bool tableExists = (long)check.ExecuteScalar()! > 0;
        using var version = connection.CreateCommand();
        version.Transaction = transaction;
        version.CommandText = "PRAGMA user_version;";
        long schemaVersion = (long)version.ExecuteScalar()!;
        if (tableExists && schemaVersion != 1)
            throw new InvalidDataException("Ukendt databaseskema. Databasen er ikke nulstillet.");
        if (!tableExists)
        {
            if (schemaVersion != 0)
                throw new InvalidDataException("Databaseskemaet er ufuldstændigt. Databasen er ikke nulstillet.");
            foreach (string file in new[] { "01_Schema.sql", "02_Seed.sql" })
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = File.ReadAllText(Path.Combine(contentRoot, "Database", file));
                command.ExecuteNonQuery();
            }
        }
        transaction.Commit();
        // En eksisterende, tom tabel får ikke nye startbøger ved genstart.
    }

    public Book[] GetAll()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Author, Pages FROM Books ORDER BY Id;";
        using var reader = command.ExecuteReader();
        var books = new List<Book>();
        while (reader.Read()) books.Add(ReadBook(reader));
        return books.ToArray();
    }

    public Book? GetById(int id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Author, Pages FROM Books WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadBook(reader) : null;
    }

    public Book Add(BookInput input)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO Books (Title, Author, Pages)
            VALUES ($title, $author, $pages)
            RETURNING Id, Title, Author, Pages;
            """;
        AddInputParameters(command, input);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException("Oprettelsen gav ingen bog tilbage.");
        // RETURNING giver id og værdier fra netop denne INSERT.
        Book book = ReadBook(reader);
        reader.Close();
        transaction.Commit();
        return book;
    }

    public Book? Update(int id, BookInput input)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE Books SET Title = $title, Author = $author, Pages = $pages
            WHERE Id = $id
            RETURNING Id, Title, Author, Pages;
            """;
        command.Parameters.AddWithValue("$id", id);
        AddInputParameters(command, input);
        using var reader = command.ExecuteReader();
        Book? book = reader.Read() ? ReadBook(reader) : null;
        reader.Close();
        transaction.Commit();
        return book;
    }

    public bool Delete(int id)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Books WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() == 1;
    }

    private static void AddInputParameters(SqliteCommand command, BookInput input)
    {
        // Parametre er data, ikke tekst der sættes ind i SQL-kommandoen.
        command.Parameters.AddWithValue("$title", input.Title?.Trim() ?? "");
        command.Parameters.AddWithValue("$author", input.Author?.Trim() ?? "");
        command.Parameters.AddWithValue("$pages", input.Pages);
    }

    private static Book ReadBook(SqliteDataReader reader) => new(
        reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3));
}
