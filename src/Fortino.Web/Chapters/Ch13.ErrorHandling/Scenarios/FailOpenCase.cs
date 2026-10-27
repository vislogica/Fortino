using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch13.Scenarios;

// Caso 13.2: il controllo del proprietario lancia un'eccezione per un utente senza cliente
// associato; la versione vulnerabile, invece di negare, lascia passare (fail open). Si prova dalla console.
public sealed class FailOpenCase : Ch13Case
{
    public override string Slug => "fail-open";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Errore che apre la porta";
    public override string Summary =>
        "Un amministratore, che non ha un cliente associato, chiede la nota riservata di un ordine altrui: il controllo del proprietario va in errore e la versione vulnerabile concede l'accesso.";
    public override string BookReference => "capitolo 13, caso 13.2";
    public override CodeLocation VulnerableCode => new("Ch13.ErrorHandling/Vulnerable/ErrorHandlingApi.cs", "fail-open-vulnerabile");
    public override CodeLocation SecureCode => new("Ch13.ErrorHandling/Secure/ErrorHandlingApi.cs", "fail-open-corretto");

    private const int ForeignOrderId = 1;   // ORD-2026-0101, di Studio Verdi (cliente 1)

    private static readonly string Send =
        "fetch('/orders/" + ForeignOrderId + "/note').then(async r => ({status: r.status, body: await r.text()})).then(console.log)";

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Anna Rossi (amministratore). Il suo profilo non ha un cliente associato.",
        "Apri la console (F12 › Console) e chiedi la nota riservata dell'ordine " + ForeignOrderId + " (di Studio Verdi), che non è suo: `" + Send + "`",
        "Guarda `status` e `body` della risposta.",
    ];

    public override ManualTrial Trial => new(
        Username: "anna",
        StartPath: "/orders",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "La risposta ha `status` 200 e nel `body` c'è la nota riservata dell'ordine di Studio Verdi: Anna la legge, anche se non è sua.",
            Proves: "Il controllo del proprietario va in errore (Anna non ha un cliente) e il codice, invece di negare, tira dritto: in caso di dubbio concede (fail open).",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch13.ErrorHandling/Vulnerable/ErrorHandlingApi.cs",
                Line: "sulla riga `int customerId = int.Parse(customerIdText!);`, nella regione `fail-open-vulnerabile`",
                Watch:
                [
                    "`customerIdText` è null (Anna non è un cliente): `int.Parse` lancia un'eccezione.",
                    "L'eccezione finisce nel `catch` vuoto, e il codice arriva lo stesso a restituire la nota.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "La risposta ha `status` 403 e il `body` è vuoto: l'accesso è negato.",
            Proves: "Il controllo parte da «non autorizzato» e, se qualcosa va storto, ci resta: in caso di dubbio nega (fail closed).",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch13.ErrorHandling/Secure/ErrorHandlingApi.cs",
                Line: "sulla riga `if (!isOwner)`, nella regione `fail-open-corretto`",
                Watch:
                [
                    "`isOwner` è rimasto false (il TryParse non ha trovato il cliente): si entra nel ramo e si risponde 403.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
