# Bogkataloget med SQLite

Et frivilligt supplement til Netværk & Distribueret design – Backend. Eksemplet er færdigt og bygger på den løste version fra dag 9. I skal ikke løse TODO-opgaver for at bruge det.

**Browser → Blazor Web → bog-API → SQLite**

Web bruger fortsat HTTP til API’et. API’et kontrollerer login, roller og bogoplysninger. SQLite erstatter JSON-filen som lagring af bøger. Brugerlogin er stadig de to lokale øvelseskonti; dette supplement flytter ikke kontiene til en database.

## Det skal I bruge

Visual Studio med .NET 10, Docker Desktop med Linux-containere og internet til første build. En separat installation af MySQL, SQL Server, SQLite eller et databaseværktøj er ikke nødvendig. NuGet-pakken følger projektets opsætning, og databasen oprettes automatisk.

## 1. Åbn den rigtige mappe

Hent hele repoet via **Code → Download ZIP** på GitHub, og pak zipfilen ud, eller klon repoet med GitHub Desktop. GitHubs zipfil og den udpakkede mappe hedder typisk `Bogkatalog_SQLite-main`. Åbn **Bogkatalog.slnx** i projektmappen i Visual Studio. Åbn derefter **View > Terminal**, og skriv:

```powershell
dir
```

I skal kunne se `compose.yaml`, `Prepare-HTTPS.ps1`, `Dockerfile` og `src`. Alle kommandoer i vejledningen køres fra denne mappe. Start ikke Web eller API med Ctrl+F5; her starter Compose begge programmer.

Stop først Web og API fra dag 9 i Docker Desktop. Begge versioner bruger de kendte lokale porte 5143 og 5443, så de kan ikke køre samtidig på disse porte. SQLite-versionen har sit eget projektnavn `bogkatalog-sqlite` og sit eget volume `sqlitedata`. Den læser eller ændrer ikke JSON-versionens data.

## 2. Klargør HTTPS

Kør som jeres normale bruger, én linje ad gangen:

```powershell
.\Prepare-HTTPS.ps1
dotnet dev-certs https --trust
```

Scriptet opretter `.env` med lokale øvelsesadgangskoder og eksporterer certifikatet til `.certs`. `--trust` beder Windows om at have tillid til udviklingscertifikatet. Godkend dialogen, hvis den vises. En eksisterende `.env` bevares.

Hvis PowerShell skriver `running scripts is disabled`, køres disse to linjer i samme terminal:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy RemoteSigned
.\Prepare-HTTPS.ps1
```

`-Scope Process` gælder kun denne terminal. `RemoteSigned` tillader lokale scripts, mens scripts markeret som hentet fra internettet normalt kræver signatur. Fortsæt derefter med `dotnet dev-certs https --trust`. Hvis scriptet stadig bliver blokeret, så sig til. Slå ikke certifikatkontrol fra.

## 3. Start løsningen

```powershell
docker compose up -d --build
```

`up` opretter og starter løsningen fra `compose.yaml`. `-d` lader containerne køre i baggrunden. `--build` bygger images, så koden og SQLite-pakken kommer med. Første gang kan det tage nogle minutter.

Åbn:

- Web: **https://localhost:5143**
- API-kontrol: **https://localhost:5443/health** → `status: ok`

Brug `localhost`, som udviklingscertifikatet passer til. Web bruger internt `http://api:8080/`. Dette interne hop bruger HTTP i den lokale øvelse, præcis som på dag 9.

## 4. Log ind og opret en bog

Åbn `.env` fra denne projektmappe i en editor. `READER_PASSWORD` er koden til `reader`, og `EDITOR_PASSWORD` er koden til `editor`. Brug ikke jeres egne private adgangskoder.

1. Log ind som `reader`, vælg **Hent bøger**, og kontrollér de seks startbøger via Forrige/Næste.
2. Prøv at oprette en bog. API’et skal afvise med **403**.
3. Log ud og ind som `editor`. Opret en bog med titlen `SQLite test`.
4. Gem det id, Web viser. En frisk database giver normalt id 7; brug altid det faktiske id.

## 5. Afprøv alle API-handlinger

Kopiér `src/Bogkatalog.Api/Bogkatalog.Api.http` til **SQLite.private.http** i samme mappe. Indsæt koderne fra `.env` i kopien. Filer med endelsen `.private.http` er udeladt fra Git og Docker-build.

- A0: health → **200**.
- A1: hent uden token → **401**.
- B1: login som reader. Kopiér `accessToken` til `readerToken`, uden anførselstegn.
- B2: hent som reader → **200**. B3: opret som reader → **403**.
- C1: login som editor. Kopiér `accessToken` til `editorToken`.
- C2: opret → **201**, Location og bogens id. Sæt `bookId` til det faktiske id.
- C3: hent netop denne bog → **200**.
- D6: opdatér → **200**. Send C3 igen for at kontrollere værdierne.
- C4: ugyldigt input → **400**.
- D3: slet → **204**. Gentag C3 eller D3 → **404**.

Send hver request via **Send Request** i Visual Studio. Koder og tokens må kun indsættes i den private kopi.

## 6. Kontrollér persistens

Opret først en ny testbog og notér dens id. Kør:

```powershell
docker compose up -d --force-recreate api
```

`--force-recreate` erstatter API-containeren. `api` vælger kun den service. Volumet bevares. Vent kort, og log ind igen. Den nye container kan have nye nøgler til tokens; det er uafhængigt af bogdatabasen.

Hent samme id med C3. Bogen skal stadig være der. Databasen ligger på `/app/data/books.db` i API-containeren. Mappen er forbundet med Docker-volumet `bogkatalog-sqlite_sqlitedata`.

SQLite-versionen bruger WAL, som er en journal til databaseændringer. Der kan derfor også ligge `books.db-wal` og `books.db-shm`. Hele `/app/data` ligger i volumet. Kopiér ikke kun en databasefil fra en kørende løsning som backup; brug en SQLite-backupmetode eller en kontrolleret nedlukning og backup af lagringen.

## 7. Se hvordan det er lavet

Læs **GENNEMGANG.md** i denne mappe. Start med:

1. `Database/01_Schema.sql` og `02_Seed.sql` i API-projektet.
2. `Services/IBookRepository.cs` og `SqliteBookRepository.cs`.
3. Registreringen og de korte handlers i API’ets `Program.cs`.
4. `BookPaging.cs` og `compose.yaml`.

Der bruges direkte SQL via ADO.NET og `Microsoft.Data.Sqlite`, ikke Entity Framework Core. Materialet fra databaseforløbet viser andre providers, bl.a. `Microsoft.Data.SqlClient` og `System.Data.SQLite`. Klasserne hedder lidt forskelligt, men forbindelser, kommandoer, parametre og readers følger samme grundidé.

## 8. Afslut

```powershell
docker compose down
```

`down` stopper og fjerner projektets containere og netværk. Det navngivne volume og bøgerne bevares. Brug ikke `down -v`, medmindre I bevidst vil slette dette projekts lokale database. Næste opstart opretter i så fald en ny database med startdata.

Projektet indeholder kode og SQL-opskriften. Jeres lokale database, koder og certifikater holdes ude af Git. JSON-data importeres ikke automatisk.

## Dokumentation

- [Microsoft.Data.Sqlite og ADO.NET](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/)
- [SQL-parametre](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/parameters)
- [Transaktioner](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions)
- [Docker Compose up](https://docs.docker.com/reference/cli/docker/compose/up/)
