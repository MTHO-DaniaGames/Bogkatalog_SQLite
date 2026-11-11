-- Køres automatisk ved første opstart. SQLite tildeler nye id'er.
CREATE TABLE Books (
    Id INTEGER PRIMARY KEY AUTOINCREMENT CHECK (Id BETWEEN 1 AND 2147483647),
    Title TEXT NOT NULL CHECK (length(trim(Title)) > 0),
    Author TEXT NOT NULL CHECK (length(trim(Author)) > 0),
    Pages INTEGER NOT NULL CHECK (Pages BETWEEN 1 AND 5000)
) STRICT;
PRAGMA user_version = 1;
