using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch13.Scenarios;

// Caso 13.1: un'operazione che fallisce restituisce al client il messaggio tecnico dell'errore,
// con nomi di tabelle e colonne del database. Si prova dalla console.
public sealed class ErrorDetailsCase : Ch13Case
{
    public override string Slug => "error-details";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Errore che rivela troppo";
    public override string Summary =>
        "Dalla console provi a emettere una fattura con un numero già usato: la versione vulnerabile restituisce il messaggio del database, che svela nomi di tabelle e colonne.";
    public override string BookReference => "capitolo 13, caso 13.1";
    public override CodeLocation VulnerableCode => new("Ch13.ErrorHandling/Vulnerable/ErrorHandlingApi.cs", "errore-dettagli-vulnerabile");
    public override CodeLocation SecureCode => new("Ch13.ErrorHandling/Secure/ErrorHandlingApi.cs", "errore-dettagli-corretto");

    private static readonly string Send =
        "fetch('/invoices/issue', {method:'POST', headers:{'Content-Type':'application/json'}, " +
        "body: JSON.stringify({year:2026, number:14})}).then(r=>r.json()).then(console.log)";

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Anna Rossi (amministratore). Apri la console (F12 › Console).",
        "Prova a emettere una fattura con un numero già usato (2026, numero 14): `" + Send + "`",
        "Guarda il campo `detail` della risposta.",
    ];

    public override ManualTrial Trial => new(
        Username: "anna",
        StartPath: "/invoices",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Nel campo `detail` arriva il testo tecnico dell'eccezione: `SqliteException`, «UNIQUE constraint failed», il nome della tabella `Invoice` e delle colonne. Un attaccante impara così com'è fatto il database.",
            Proves: "Il messaggio tecnico dell'errore, pensato per lo sviluppatore, viene spedito così com'è al client.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch13.ErrorHandling/Vulnerable/ErrorHandlingApi.cs",
                Line: "sulla riga `string errorDetails = ex.ToString();`, nella regione `errore-dettagli-vulnerabile`",
                Watch:
                [
                    "`errorDetails` contiene il testo completo dell'eccezione del database (tabella, colonne, righe di codice): è quello che finisce nella risposta.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Nel campo `detail` arriva solo «Operazione non riuscita. Riferimento per l'assistenza: ...», con un codice breve. Il dettaglio tecnico non c'è: è finito solo nel registro interno (pagina «Registro»).",
            Proves: "Al client va un messaggio generico con un riferimento; il dettaglio tecnico resta sul server, nel registro, per chi deve indagare.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch13.ErrorHandling/Secure/ErrorHandlingApi.cs",
                Line: "sulla riga `logger.LogWarning(ex, ...)`, nella regione `errore-dettagli-corretto`",
                Watch:
                [
                    "Il dettaglio dell'eccezione va a `logger` (il registro), mentre al client parte solo `message`, senza dettagli.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
