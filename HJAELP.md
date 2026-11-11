# Hjælp til SQLite-versionen

## Porten er optaget

Stop dag 9-versionens API og Web i Docker Desktop. Begge eksempler bruger 5443 og 5143. Den nye løsning skal hedde `bogkatalog-sqlite` i Docker Desktop.

## PowerShell blokerer scriptet

Kør i samme terminal, én linje ad gangen:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy RemoteSigned
.\Prepare-HTTPS.ps1
```

Fortsæt med `dotnet dev-certs https --trust`. Hvis scriptet stadig blokeres, så sig til. Det kan fx skyldes regler på pc’en eller filens markering som downloadet.

## Docker finder ikke filer

Kør `dir`. I skal stå i mappen med `compose.yaml`, `Dockerfile` og `src`. Kontrollér, at I arbejder i den udpakning, I har åbnet i Visual Studio.

## Pakken kan ikke hentes

Første build kræver internetadgang til NuGet og Microsofts container-images. Læs den første egentlige fejl i build-outputtet. I skal ikke installere MySQL for at rette dette.

## Web kan ikke kontakte API’et

Vent kort efter opstart, og prøv igen. Hvis det fortsætter:

```powershell
docker compose ps -a
docker compose logs --tail 40 api web
```

`ps` viser containerstatus. `-a` tager også stoppede containere med. `logs` viser programbeskeder; `--tail 40` begrænser dem til de sidste 40 linjer fra hver af de valgte services.

Web skal bruge `http://api:8080/` inde i Docker-netværket. Browseren bruger `https://localhost:5143`. Kontrollér også `https://localhost:5443/health`.

## 401 eller 403

401: Log ind igen og kopiér et nyt token, hvis I bruger den private requestfil. Efter udskiftning af API-containeren kan gamle tokens blive ugyldige.

403: Kontrollen virker, hvis reader forsøger at oprette, ændre eller slette. Log ind som editor for de handlinger.

## Bogen ses ikke på første listeside

Der vises to bøger pr. side. Brug Næste, eller hent bogen med dens id via C3. Brug id’et fra svaret, ikke et gættet id.

## Databasefejl

Læs API-loggen. En ulæselig eller beskadiget database bliver ikke automatisk erstattet med startdata. Undersøg årsagen og bevar data, før I overvejer at starte forfra. Fjern ikke volumet som et generelt fejlfindingstrin.

## Ændret kode slår ikke igennem

Gem filerne og kør `docker compose up -d --build` fra samme projektmappe. Almindelig stop/start bygger ikke nye images.

## Hvor er databasen?

I Docker ligger den på `/app/data/books.db`, som er forbundet med `bogkatalog-sqlite_sqlitedata`. Den ligger ikke i det udleverede repo. I Docker Desktop kan I finde volumet under Volumes. Et separat databaseværktøj er valgfrit, ikke en forudsætning.
