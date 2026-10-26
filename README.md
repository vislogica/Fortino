# A prova di attacco — il progetto di esempio

Questo progetto accompagna il libro **"A prova di attacco — Sicurezza delle applicazioni .NET,
vulnerabilità per vulnerabilità, nell'era dell'AI"** (Vis Logica, collana *Sviluppare nell'era
dell'AI*).

Contiene **Fortino**, il gestionale di *Carta & Co.*, un distributore immaginario di forniture per
ufficio. Ogni vulnerabilità del libro si prova nella sezione **Laboratorio**: prima sulla versione
vulnerabile del codice, poi su quella corretta.

> **Attenzione.** Il codice nelle cartelle `Chapters/*/Vulnerable` è volutamente insicuro. Serve a imparare:
> non copiarlo in un'applicazione vera, e non pubblicare Fortino su un server raggiungibile da altri.

## Cosa serve
- .NET SDK 10
- Visual Studio 2022 (17.14 o successivo), Visual Studio 2026, oppure VS Code con C# Dev Kit
- Nient'altro: il database è SQLite e viene creato al primo avvio in `src/Fortino.Web/App_Data/`.

## Come si avvia
1. Una volta sola, rendi attendibile il certificato https di sviluppo:
   ```
   dotnet dev-certs https --trust
   ```
2. Apri `Fortino.sln` e avvia il progetto `Fortino.Web` (profilo `https`),
   oppure da riga di comando:
   ```
   dotnet run --project src/Fortino.Web --launch-profile https
   ```
3. Apri https://localhost:5001 e accedi con uno degli utenti qui sotto.

## Utenti dimostrativi
La password è la stessa per tutti: `Fortino-Demo-2026`.

| Utente | Nome | Ruolo |
|---|---|---|
| `anna` | Anna Rossi | Amministratore |
| `mario` | Mario Bianchi | Operatore |
| `verdi` | Studio Verdi | Cliente (portale) |
| `lumen` | Ottica Lumen | Cliente (portale) |

Una volta dentro, il menu in alto a destra (**Entra come…**) cambia utente senza password.
Esiste solo in questa applicazione dimostrativa ed è attivo solo in ambiente Development.

## Come si usa il Laboratorio
1. Apri **Laboratorio** dal menu e scegli un capitolo.
2. Scegli un caso. L'interruttore in alto sceglie la versione: **Vulnerabile** o **Corretto**.
3. Premi **Prova codice vulnerabile** o **Prova codice corretto** (la scritta segue l'interruttore):
   i passi compaiono uno per riga, con l'esito in fondo.
   La prova entra da sola con l'utente scritto sopra il pulsante: l'utente scelto con **Entra come…**
   vale solo per le prove a mano.
4. Per seguire l'attacco nel codice, metti un breakpoint nel file indicato nel riquadro a destra.
5. Dopo ogni prova, **Ripristina i dati** riporta il database allo stato iniziale.

## Test automatici
```
dotnet test
```
Per ogni caso ci sono due test: uno dimostra che l'attacco riesce sulla versione vulnerabile,
l'altro che viene bloccato su quella corretta.

## Struttura
```
src/Fortino.Web/
  Domain/        il dominio di Carta & Co. (clienti, ordini, fatture, utenti)
  Data/          database SQLite: schema, dati iniziali, letture comuni
  Auth/          accesso e uscita (versione corretta)
  Infrastructure/ posta finta e registro in memoria
  Lab/           la parte comune del Laboratorio
  Chapters/      una cartella per capitolo: Vulnerable/, Secure/, Scenarios/
  Components/    pagine Blazor (MudBlazor)
tests/Fortino.Tests/
```

## Licenza
Da definire.
