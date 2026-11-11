# Fra JSON-fil til SQLite

## Hvad ændrer sig?

I JSON-versionen holder `BookStore` en liste og skriver hele listen til `books.json`. Her ligger bøgerne i tabellen `Books`. Et repository sender SQL til SQLite og omdanner resultaterne til de samme `Book`-objekter.

Web bruger stadig `BookApiClient`, HTTP og JSON-beskeder. **JSON er stadig formatet på API’ets svar, selvom lagringen nu er en database.** JSON som beskedformat og JSON som lagringsfil er to forskellige valg.

SQLite er en relationel database, selvom den ligger i en lokal fil. Databasemotoren kører inde i API-processen. Der er derfor ingen separat databasecontainer, serveradresse eller databaseport i dette eksempel. Ved MySQL ville API’et normalt kontakte en separat databaseserver.

## 1. Tabellen

`src/Bogkatalog.Api/Database/01_Schema.sql` opretter `Books` med `Id`, `Title`, `Author` og `Pages`.

- `Id` er primærnøgle. `AUTOINCREMENT` sørger i denne tabel for, at tidligere tildelte id’er fra gennemførte inserts ikke genbruges efter sletning.
- `NOT NULL` kræver værdier. `CHECK` begrænser bl.a. sidetallet til 1–5000.
- `STRICT` får SQLite til at håndhæve tabellens datatyper strengere. SQLite understøtter forskellige typer tabeller; denne bruger STRICT.
- API’ets `BookRules` validerer fortsat input og giver et forståeligt 400-svar. Databasens regler er ekstra beskyttelse af de gemte data.

`02_Seed.sql` indsætter seks kendte startbøger. Repositoryets initialisering kører skema og seed samlet i en transaktion, når tabellen oprettes første gang. Sletter I alle bøger, forbliver tabellen tom efter genstart. Startdata indsættes ikke ved hver opstart. `user_version` markerer dette eksempels skemaversion; her er ikke et system til automatiske skemaopgraderinger.

## 2. Repository og dependency injection

`IBookRepository` beskriver de handlinger, API’et har brug for. SQL-koden ligger i `SqliteBookRepository`. Registreringen i `Program.cs` forbinder dem:

```csharp
builder.Services.AddSingleton<IBookRepository, SqliteBookRepository>();
```

Når en handler har en `IBookRepository`-parameter, leverer ASP.NET Core den registrerede implementering. Repository-objektet er fælles, men hver metode åbner sin egen databaseforbindelse. En åben forbindelse deles ikke mellem samtidige requests.

Det er API’et, der kalder repositoryet. Web får ikke databaseforbindelsen og skal ikke kende tabellerne.

## 3. Hent én bog

I `GetById` står:

```csharp
command.CommandText = "SELECT Id, Title, Author, Pages FROM Books WHERE Id = $id;";
command.Parameters.AddWithValue("$id", id);
using var reader = command.ExecuteReader();
return reader.Read() ? ReadBook(reader) : null;
```

`SELECT` vælger kolonnerne. `WHERE` begrænser opslaget til det ønskede id. `$id` er en parameter: værdien sendes separat fra SQL-teksten. I materialet har I også set parametre med `@`; denne provider understøtter bl.a. `$` og `@`.

`ExecuteReader` udfører forespørgslen. `Read()` går til den første række og giver false, hvis der ikke er nogen. `ReadBook` læser kolonnerne i SELECT-rækkefølgen og opretter en `Book`.

Repositoryet returnerer en bog eller null. Handleren vælger stadig HTTP 200 eller 404. Databaseadgang og valg af HTTP-svar ligger dermed hver sit sted.

## 4. Opret og opdatér

`INSERT INTO` opretter en række med titel, forfatter og sidetal. SQLite tildeler id’et. `RETURNING Id, Title, Author, Pages` giver værdierne fra netop denne ændring tilbage. Derfor behøver vi ikke gætte id eller hente rækken med det højeste id.

`UPDATE ... WHERE Id = $id RETURNING ...` ændrer den valgte bog og returnerer dens nye værdier. Ingen række betyder, at id’et ikke findes.

Oprettelse og opdatering bruger en transaktion. Readeren lukkes, før `Commit()` gennemfører ændringen. Hvis operationen fejler før commit, gennemføres transaktionen ikke. Først efter vellykket lagring returnerer repositoryet til handleren, som sender HTTP-svaret.

Titel og forfatter kan godt indeholde apostroffer. De bliver sendt som parametre og ikke sat sammen med SQL ved hjælp af tekstsammenkædning. Parametre beskytter mod, at input bliver udført som SQL; de erstatter ikke validering af bogoplysninger eller adgangskontrol.

## 5. Slet

`DELETE FROM Books WHERE Id = $id` fjerner højst den valgte række. `ExecuteNonQuery()` returnerer antallet af berørte rækker. Repositoryet giver true ved én slettet række. Handleren oversætter det til 204; ellers bliver det 404.

## 6. using og forbindelser

`using var` kalder `Dispose`, når metoden forlades, også ved en exception. Det rydder op i readers, kommandoer, transaktioner og forbindelser. Providerens connection pooling kan genbruge en underliggende forbindelse efter oprydningen; det betyder ikke, at metoden fortsat ejer den.

Her bruges synkrone databasekald for at holde ADO.NET-eksemplet tæt på databaseforløbets tilgang. Det er et lille undervisningseksempel, ikke en skabelon til høj belastning.

## 7. Pagination og søgning

`GetAll()` bruger SQL til at hente rækker i id-rækkefølge. Den eksisterende `BookPaging` filtrerer og opdeler bagefter samlingen i C# ligesom før. `page` er listesiden, `pageSize` er bøger pr. listeside, og bogens `Pages` er antal sider i bogen.

Denne version viser udskiftning af lagringen og bevarer API’ets eksisterende adfærd. Ved større datamængder bør filtrering, optælling og pagination udføres i databasen, så API’et ikke henter alle rækker først. Det er en mulig videreudvikling; den er ikke implementeret her.

## 8. Persistens og samtidighed

`BookStorage__Path` vælger `/app/data/books.db`. Mountet `sqlitedata:/app/data` forbinder mappen med Docker-volumet. Databasemotoren skriver data, mens Docker bevarer lagringsområdet gennem en udskiftning af containeren.

SQLite håndterer låsning og transaktioner. Her er WAL aktiveret, så læsere og en skriver bedre kan arbejde samtidig. Der er stadig kun én aktiv skriver ad gangen. Lange transaktioner kan give ventetid og til sidst en databasefejl. Repositoryet bruger ingen fælles C#-lås til selve CRUD-kaldene.

En database forhindrer heller ikke automatisk, at en bruger overskriver en anden brugers ændring med gammel information. Det kræver fx versionskontrol på rækkerne. Eksemplet tilføjer ikke dette og er fortsat tænkt til én API-container på én pc. Flere værter og mange skrivninger kræver en anden vurdering af lagring og drift.

## 9. Hvad kan I tage med til projektet?

Har jeres Blazor-projekt allerede et database-repository, kan I bruge samme fordeling af ansvar: API’et bruger repositoryet, og Blazor bruger API’et. Der kan blive behov for at tilpasse typer, registrering og metoder. Databaseadgangen skal ikke kopieres ind i Webs API-client.

Har I valgt MySQL eller SQL Server, behøver I ikke skifte til SQLite. HTTP-grænsen mellem Web og API kan være den samme, selvom databaseprovider og SQL-syntaks er anderledes.

## Kilder

- https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/
- https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/parameters
- https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions
- https://www.sqlite.org/lang_returning.html
- https://www.sqlite.org/stricttables.html
- https://www.sqlite.org/wal.html
