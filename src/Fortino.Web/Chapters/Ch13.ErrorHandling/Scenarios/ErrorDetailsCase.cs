using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch13.Scenarios;

// Caso 13.1: un'operazione che fallisce restituisce al client il messaggio tecnico dell'errore,
// con nomi di tabelle e colonne del database. Si prova dal riquadro «Emetti la fattura» di una bozza.
public sealed class ErrorDetailsCase : Ch13Case
{
    public override string Slug => "error-details";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Errore che rivela troppo";
    public override string Summary =>
        "Emetti una fattura in bozza con un numero già usato: la versione vulnerabile mostra nella finestra di errore il messaggio del database, che svela nomi di tabelle, colonne e percorsi del server.";
    public override string BookReference => "capitolo 13, caso 13.1";
    public override CodeLocation VulnerableCode => new("Ch13.ErrorHandling/Vulnerable/ErrorHandlingApi.cs", "errore-dettagli-vulnerabile");
    public override CodeLocation SecureCode => new("Ch13.ErrorHandling/Secure/ErrorHandlingApi.cs", "errore-dettagli-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Anna Rossi (Amministratrice), nella pagina «Fatture». La fattura 2026/014 esiste già.",
        "Clicca «Modifica» sulla bozza di Panificio Sole. In fondo alla pagina c'è il riquadro «Emetti la fattura», con il primo numero libero già scritto.",
        "Scrivi 14 nel numero, cioè un numero già usato, e premi «Emetti».",
        "Sopra il modulo si apre una finestra di errore, con l'icona rossa: leggi il messaggio.",
    ];

    public override ManualTrial Trial => new(
        Username: "anna",
        StartPath: "/invoices",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "La finestra di errore mostra in rosso il testo tecnico dell'eccezione: `SqliteException`, «UNIQUE constraint failed», il nome della tabella `Invoice` e delle colonne, i percorsi dei file del server. Un attaccante impara così com'è fatto il database.",
            Proves: "Il messaggio tecnico dell'errore, pensato per lo sviluppatore, viene mostrato così com'è a chi usa il programma.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch13.ErrorHandling/Vulnerable/ErrorHandlingApi.cs",
                Line: "sulla riga `string errorDetails = ex.ToString();`, nella regione `errore-dettagli-vulnerabile`",
                Watch:
                [
                    "`errorDetails` contiene il testo completo dell'eccezione del database (tabella, colonne, righe di codice): è quello che finisce nella finestra di errore.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "La finestra di errore dice solo «Operazione non riuscita. Riferimento per l'assistenza: ...», con un codice breve. Il dettaglio tecnico non c'è: è finito solo nel registro interno (pagina «Registro»).",
            Proves: "All'utente va un messaggio generico con un riferimento; il dettaglio tecnico resta sul server, nel registro, per chi deve indagare.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch13.ErrorHandling/Secure/ErrorHandlingApi.cs",
                Line: "sulla riga `logger.LogWarning(ex, ...)`, nella regione `errore-dettagli-corretto`",
                Watch:
                [
                    "Il dettaglio dell'eccezione va a `logger` (il registro), mentre all'utente arriva solo `message`, senza dettagli.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
