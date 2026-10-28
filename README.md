# A prova di attacco — Fortino

**This README is available in two languages · Questo README è disponibile in due lingue**

| | |
|---|---|
| 🇬🇧 **[English](#english)** | Read the instructions in English |
| 🇮🇹 **[Italiano](#italiano)** | Leggi le istruzioni in italiano |

> The Fortino application, its pages and the Lab are in Italian, like the book.
> L'applicazione Fortino, le sue pagine e il Laboratorio sono in italiano, come il libro.

---

<a id="english"></a>
# 🇬🇧 English

This project accompanies the book **"A prova di attacco — Sicurezza delle applicazioni .NET,
vulnerabilità per vulnerabilità, nell'era dell'AI"** (*Under attack — .NET application security,
vulnerability by vulnerability, in the age of AI*; Vis Logica, series *Sviluppare nell'era dell'AI*).

It contains **Fortino**, the management software of *Carta & Co.*, a fictional office supplies
distributor. Every vulnerability in the book can be tried in the **Laboratorio** (Lab) section: first on
the vulnerable version of the code, then on the fixed one. The trials are done **by hand, in the real
pages** of the application, the way an attacker would: an address changed in the browser bar, a form
filled in, a file uploaded, two tabs open on the same page.

> **Warning.** The code in the `Chapters/*/Vulnerable` folders is insecure on purpose. It is meant for
> learning: do not copy it into a real application, and do not publish Fortino on a server others can reach.

## Requirements
- .NET SDK 10
- Visual Studio 2022 (17.14 or later), Visual Studio 2026, or VS Code with C# Dev Kit
- A recent browser (Chrome, Edge or Firefox)
- Nothing else: the database is SQLite and is created on first run in `src/Fortino.Web/App_Data/`.

## How to start
1. Once only, trust the https development certificate:
   ```
   dotnet dev-certs https --trust
   ```
2. Open `Fortino.sln` and run the `Fortino.Web` project (`https` profile),
   or from the command line:
   ```
   dotnet run --project src/Fortino.Web --launch-profile https
   ```
3. Open https://localhost:5001 and sign in with one of the users below.

The `https` profile opens two addresses:

| Address | What is there |
|---|---|
| `https://localhost:5001` | Fortino, the management software |
| `https://localhost:5002` | the **scammer's site**, used in the trials of chapters 4 and 15. Fortino itself serves it, but for the browser it is a different site |

If you start Fortino another way, keep both addresses open, or the trials of chapters 4 and 15 will not work:
```
dotnet run --project src/Fortino.Web --urls "https://localhost:5001;https://localhost:5002"
```

## Demo users
The password is the same for everyone: `Fortino-Demo-2026`.

| User | Name | Role |
|---|---|---|
| `anna` | Anna Rossi | Administrator |
| `mario` | Mario Bianchi | Operator |
| `verdi` | Studio Verdi | Customer (portal) |
| `lumen` | Ottica Lumen | Customer (portal) |

Once signed in, the menu at the top right (**Entra come…**, "sign in as…") switches user without a
password. It exists only in this demo application and only in the Development environment.

## How to use the Lab
The Lab opens even without signing in: https://localhost:5001/lab

1. Open a chapter. Each case has two panels, **Versione vulnerabile** (vulnerable version) and **Versione
   corretta** (fixed version), with the steps to follow, what should happen, what it proves and where to
   put the breakpoint in the debugger.
2. Press **Inizia la prova** (start the trial) on the version you want to see: you are signed out and land
   on the sign-in page. The trial's user name and password are already filled in, so you do not have to
   type them at every trial (a real application must never do this); you can change them. The eye next to
   the password shows it.
3. Follow the steps by hand, in the pages of the application. The banner at the top always shows which
   case and which version of the code are answering; **Mostra i passi** (show the steps) opens them again.
4. At the end press **Ho fatto la prova** (I did the trial) in the banner: you go back to the chapter, the
   trial is marked as done and the database goes back to its initial state. Every **Inizia la prova** starts
   from clean data anyway.
5. To follow the attack in the code, run Fortino with the debugger (F5), set the breakpoint shown in the
   panel and repeat the last step.

Outside a trial, the fixed version of the code always answers.

### The tools used in the trials
The trials use only pages and tools any user already has:

| Tool | Where | Chapters |
|---|---|---|
| The address bar | the browser | 1, 7, 8, 9, 10, 15, 18 |
| Forms and buttons of the real pages | the management software and the customer portal | almost all |
| Two tabs on the same page | right-click on the tab › *Duplicate* | 3, 7, 11, 16 |
| The developer tools (F12) | *Elements* tab (7.4), *Console* tab (8.1, 11.2) | 7, 8, 11 |
| Test files to download | `/lab/file/...`, given in the steps | 10, 18 |
| The scammer's site | `https://localhost:5002` (pages of chapters 4 and 15, `/pannello`) | 4, 15 |
| **Posta finta** (fake mailbox) | *Strumenti* menu: the emails Fortino "sends" | 12, 15 |
| **Registro** (log) | *Strumenti* menu: the application's internal log | 13, 14 |
| **Vista database** (database view) | administrator menu, Lab only | 12 |

The browser console remains only in the two cases where it is the attacker's actual weapon: a field no
page sends (8.1) and a method the page exposes to JavaScript (11.2).

Two simplifications, also explained in the book:
- **Chapter 16 (race condition).** In reality the window between check and write lasts milliseconds. In
  the Lab it lasts **3 seconds**, in both versions, so it can be opened by hand with two tabs.
- **Chapter 15.2 (Host header).** A browser cannot forge that header: the scammer's panel does it, in
  place of the tool a real attacker would use.

Chapters 5 and 6 are about **process** (vulnerable components, secrets in code, Data Protection keys):
there is nothing to try in the browser, and the case panel lists the steps to take on the project.

### The cases
| Chapter | Cases |
|---|---|
| 1 · Access control | 1.1 Another customer's order · 1.2 Rule only in the user interface |
| 2 · Injection | 2.1 SQL injection in the search · 2.2 Script in the order note |
| 3 · Authentication | 3.1 Passwords tried in bulk · 3.2 Session that never dies |
| 4 · Security headers | 4.1 Page that can be framed (clickjacking) · 4.2 CORS too open |
| 5 · Supply chain | Components with known vulnerabilities |
| 6 · Secrets | 6.1 Secret in the code · 6.2 Ephemeral Data Protection keys |
| 7 · Business logic | 7.1 Issued invoice can be edited · 7.2 Data not frozen · 7.3 Skipped state · 7.4 Negative quantity |
| 8 · Mass assignment | 8.1 Customer who becomes administrator · 8.2 Price chosen by the customer |
| 9 · Path traversal | 9.1 Path traversal on upload · 9.2 Path traversal on download |
| 10 · Resource consumption | 10.1 Image with huge dimensions · 10.2 Slow regular expression · 10.3 Export without limits |
| 11 · Blazor Server | 11.1 Another customer's invoices · 11.2 Method called from the console |
| 12 · Weak cryptography | 12.1 Password with a weak hash · 12.2 Predictable reset code |
| 13 · Error handling | 13.1 Error that reveals too much · 13.2 Error that opens the door |
| 14 · Logging and audit | 14.1 Secrets in the log · 14.2 Sensitive action not tracked |
| 15 · Open redirect and Host header | 15.1 Open redirect · 15.2 Link poisoned by the Host header |
| 16 · Race condition | 16.1 Coupon used more than once · 16.2 Double invoice for one order |
| 17 · AI risks | 17.1 Instruction hidden in the data · 17.2 Assistant with too much power |
| 18 · Deserialization and XXE | 18.1 XML that brings its own definition · 18.2 The data choose the type |

## Automated tests
```
dotnet test
```
The tests repeat what the reader does: they start the trial, sign in with the case's user and use the
real pages (forms with their antiforgery token, addresses, test files). For each case one test shows that
the attack succeeds on the vulnerable version, another that it is blocked on the fixed one.

## Structure
```
src/Fortino.Web/
  Domain/         the Carta & Co. domain (customers, orders, invoices, users)
  Data/           SQLite database: schema, initial data, shared queries
  Auth/           sign-in and sign-out
  Infrastructure/ fake mailbox, in-memory log, shared page actions
  Lab/            the shared part of the Lab (manual trials, trial cookie, data reset)
  Chapters/       one folder per chapter: Vulnerable/, Secure/, Scenarios/ (see Chapters/README.md)
  Components/     Blazor pages (MudBlazor)
  wwwroot/lab/    page scripts used by some trials (chapters 11, 13, 17)
tests/Fortino.Tests/  one test file per chapter
```

## License
To be defined.

[↑ Back to the top](#a-prova-di-attacco--fortino) · 🇮🇹 [Versione italiana](#italiano)

---

<a id="italiano"></a>
# 🇮🇹 Italiano

Questo progetto accompagna il libro **"A prova di attacco — Sicurezza delle applicazioni .NET,
vulnerabilità per vulnerabilità, nell'era dell'AI"** (Vis Logica, collana *Sviluppare nell'era
dell'AI*).

Contiene **Fortino**, il gestionale di *Carta & Co.*, un distributore immaginario di forniture per
ufficio. Ogni vulnerabilità del libro si prova nella sezione **Laboratorio**: prima sulla versione
vulnerabile del codice, poi su quella corretta. Le prove si fanno **a mano, nelle pagine vere** del
gestionale, come le farebbe un attaccante: un indirizzo cambiato nella barra, un modulo compilato,
un file caricato, due schede aperte sulla stessa pagina.

> **Attenzione.** Il codice nelle cartelle `Chapters/*/Vulnerable` è volutamente insicuro. Serve a imparare:
> non copiarlo in un'applicazione vera, e non pubblicare Fortino su un server raggiungibile da altri.

## Cosa serve
- .NET SDK 10
- Visual Studio 2022 (17.14 o successivo), Visual Studio 2026, oppure VS Code con C# Dev Kit
- Un browser recente (Chrome, Edge o Firefox)
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

Il profilo `https` apre due indirizzi:

| Indirizzo | Cosa c'è |
|---|---|
| `https://localhost:5001` | Fortino, il gestionale |
| `https://localhost:5002` | il **sito del truffatore**, usato nelle prove dei capitoli 4 e 15. Lo serve lo stesso Fortino, ma per il browser è un altro sito |

Se avvii Fortino in un altro modo, tieni aperti tutti e due gli indirizzi, altrimenti le prove dei
capitoli 4 e 15 non funzionano:
```
dotnet run --project src/Fortino.Web --urls "https://localhost:5001;https://localhost:5002"
```

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
Il Laboratorio si apre anche senza essere entrati: https://localhost:5001/lab

1. Apri un capitolo. Ogni caso ha due riquadri, **Versione vulnerabile** e **Versione corretta**, con i
   passi da fare, cosa deve succedere, cosa dimostra e dove mettere il punto di interruzione nel debugger.
2. Premi **Inizia la prova** sulla versione che vuoi vedere: esci dall'applicazione e arrivi alla pagina
   di accesso. Nome utente e password della prova sono già scritti nei campi, per non riscriverli a ogni
   prova (in un programma vero non si fa mai); puoi cambiarli. L'occhio accanto alla password la mostra.
3. Fai i passi a mano, nelle pagine del gestionale. La fascia in alto ricorda sempre quale caso e quale
   versione del codice stanno rispondendo; **Mostra i passi** li riapre.
4. Alla fine premi **Ho fatto la prova** nella fascia: torni al capitolo, la prova risulta fatta e il
   database torna allo stato iniziale. Ogni **Inizia la prova** riparte comunque da dati puliti.
5. Per seguire l'attacco nel codice, avvia Fortino con il debug (F5), metti il punto di interruzione
   indicato nel riquadro e rifai l'ultimo passo.

Fuori da una prova risponde sempre la versione corretta del codice.

### Gli strumenti delle prove
Le prove usano solo pagine e strumenti che un utente ha già a disposizione:

| Strumento | Dove | Capitoli |
|---|---|---|
| La barra degli indirizzi | il browser | 1, 7, 8, 9, 10, 15, 18 |
| Moduli e pulsanti delle pagine vere | il gestionale e il portale clienti | quasi tutti |
| Due schede sulla stessa pagina | tasto destro sulla scheda › *Duplica* | 3, 7, 11, 16 |
| Gli strumenti per sviluppatori (F12) | scheda *Elements* (7.4), scheda *Console* (8.1, 11.2) | 7, 8, 11 |
| File di prova da scaricare | `/lab/file/...`, indicati nei passi | 10, 18 |
| Il sito del truffatore | `https://localhost:5002` (pagine dei capitoli 4 e 15, `/pannello`) | 4, 15 |
| **Posta finta** | menu *Strumenti*: le email che Fortino «spedisce» | 12, 15 |
| **Registro** | menu *Strumenti*: il registro interno dell'applicazione | 13, 14 |
| **Vista database** | menu dell'amministratore, solo nel Laboratorio | 12 |

La console del browser resta solo nei due casi in cui è proprio l'arma dell'attaccante: un campo che
nessuna pagina manda (8.1) e un metodo che la pagina espone a JavaScript (11.2).

Due semplificazioni, spiegate anche nel libro:
- **Capitolo 16 (race condition).** Nella realtà la finestra tra controllo e scrittura dura millisecondi.
  Nel Laboratorio dura **3 secondi**, in tutte e due le versioni, per poterla aprire a mano con due schede.
- **Capitolo 15.2 (intestazione Host).** Un browser non può falsificare quell'intestazione: lo fa il
  pannello del truffatore, al posto dello strumento che userebbe un attaccante vero.

I capitoli 5 e 6 sono **di processo** (componenti vulnerabili, segreti nel codice, chiavi di Data
Protection): non c'è niente da provare nel browser, e la scheda del caso elenca i passi da fare sul
progetto.

### I casi
| Capitolo | Casi |
|---|---|
| 1 · Controllo degli accessi | 1.1 Ordine di un altro cliente · 1.2 Regola solo nell'interfaccia |
| 2 · Iniezioni | 2.1 SQL injection nella ricerca · 2.2 Script nella nota dell'ordine |
| 3 · Autenticazione | 3.1 Password provate a tappeto · 3.2 Sessione che non muore |
| 4 · Intestazioni di sicurezza | 4.1 Pagina incorniciabile (clickjacking) · 4.2 CORS troppo aperto |
| 5 · Catena di fornitura | Componenti con vulnerabilità note |
| 6 · Segreti | 6.1 Segreto nel codice · 6.2 Chiavi di Data Protection effimere |
| 7 · Logica di business | 7.1 Fattura emessa modificabile · 7.2 Dati non congelati · 7.3 Salto di stato · 7.4 Quantità negativa |
| 8 · Mass assignment | 8.1 Cliente che diventa amministratore · 8.2 Prezzo deciso dal cliente |
| 9 · Path traversal | 9.1 Path traversal in caricamento · 9.2 Path traversal in scaricamento |
| 10 · Consumo di risorse | 10.1 Immagine con dimensioni enormi · 10.2 Espressione regolare lenta · 10.3 Esportazione senza limiti |
| 11 · Blazor Server | 11.1 Fatture di un altro cliente · 11.2 Metodo chiamato dalla console |
| 12 · Crittografia debole | 12.1 Password con hash debole · 12.2 Codice di reset prevedibile |
| 13 · Gestione degli errori | 13.1 Errore che rivela troppo · 13.2 Errore che apre la porta |
| 14 · Log e audit | 14.1 Segreti nel registro · 14.2 Azione sensibile non tracciata |
| 15 · Open redirect e Host header | 15.1 Reindirizzamento aperto · 15.2 Link avvelenato dall'Host |
| 16 · Race condition | 16.1 Buono sconto usato più volte · 16.2 Doppia fattura per un ordine |
| 17 · Rischi dell'AI | 17.1 Istruzione nascosta nei dati · 17.2 Assistente con troppi poteri |
| 18 · Deserializzazione e XXE | 18.1 XML che porta una sua definizione · 18.2 Il tipo lo scelgono i dati |

## Test automatici
```
dotnet test
```
I test rifanno i gesti del lettore: iniziano la prova, entrano con l'utente del caso e usano le pagine
vere (moduli con il loro token antiforgery, indirizzi, file di prova). Per ogni caso un test dimostra che
l'attacco riesce sulla versione vulnerabile, un altro che viene bloccato su quella corretta.

## Struttura
```
src/Fortino.Web/
  Domain/         il dominio di Carta & Co. (clienti, ordini, fatture, utenti)
  Data/           database SQLite: schema, dati iniziali, letture comuni
  Auth/           accesso e uscita
  Infrastructure/ posta finta, registro in memoria, azioni comuni delle pagine
  Lab/            la parte comune del Laboratorio (prove a mano, cookie della prova, ripristino dei dati)
  Chapters/       una cartella per capitolo: Vulnerable/, Secure/, Scenarios/ (vedi Chapters/README.md)
  Components/     pagine Blazor (MudBlazor)
  wwwroot/lab/    gli script delle pagine usati da alcune prove (capitoli 11, 13, 17)
tests/Fortino.Tests/  un file di test per capitolo
```

## Licenza
Da definire.

[↑ Torna all'inizio](#a-prova-di-attacco--fortino) · 🇬🇧 [English version](#english)
