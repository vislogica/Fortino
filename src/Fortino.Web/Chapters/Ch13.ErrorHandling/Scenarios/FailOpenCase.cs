using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch13.Scenarios;

// Caso 13.2: il controllo del proprietario lancia un'eccezione per un utente senza cliente
// associato; la versione vulnerabile, invece di negare, lascia passare (fail open). Si prova dal pulsante «Mostra la nota riservata» della pagina dell'ordine.
public sealed class FailOpenCase : Ch13Case
{
    public override string Slug => "fail-open";
    public override IReadOnlyList<string> LabUsers => ["mario"];
    public override string Title => "Errore che apre la porta";
    public override string Summary =>
        "Un operatore, che non ha un cliente associato, chiede la nota riservata di un ordine di un cliente: il controllo del proprietario va in errore e la versione vulnerabile concede l'accesso.";
    public override string BookReference => "capitolo 13, caso 13.2";
    public override CodeLocation VulnerableCode => new("Ch13.ErrorHandling/Vulnerable/ErrorHandlingApi.cs", "fail-open-vulnerabile");
    public override CodeLocation SecureCode => new("Ch13.ErrorHandling/Secure/ErrorHandlingApi.cs", "fail-open-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Mario Bianchi (Operatore). Il suo profilo non ha un cliente associato: è un dipendente di Carta & Co.",
        "Apri «Ordini» e poi l'ordine ORD-2026-0101 di Studio Verdi.",
        "In fondo c'è «Nota riservata del cliente»: la legge solo il cliente che ha fatto l'ordine. Il pulsante però c'è per tutti, e chi può leggerla lo decide il server. Premi «Mostra la nota riservata».",
    ];

    public override ManualTrial Trial => new(
        Username: "mario",
        StartPath: "/orders",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Sotto il pulsante compare, in giallo, la nota riservata dell'ordine di Studio Verdi: Mario la legge, anche se non è sua.",
            Proves: "Il controllo del proprietario va in errore (Mario non ha un cliente) e il codice, invece di negare, tira dritto: in caso di dubbio concede (fail open).",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch13.ErrorHandling/Vulnerable/ErrorHandlingApi.cs",
                Line: "sulla riga `int customerId = int.Parse(customerIdText!);`, nella regione `fail-open-vulnerabile`",
                Watch:
                [
                    "`customerIdText` è null (Mario non è un cliente): `int.Parse` lancia un'eccezione.",
                    "L'eccezione finisce nel `catch` vuoto, e il codice arriva lo stesso a restituire la nota.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Sotto il pulsante compare, in rosso, «Accesso negato»: il server ha risposto 403 e la nota non arriva.",
            Proves: "Il controllo parte da «non autorizzato» e, se qualcosa va storto, ci resta: in caso di dubbio nega (fail closed).",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch13.ErrorHandling/Secure/ErrorHandlingApi.cs",
                Line: "sulla riga `if (!isOwner)`, nella regione `fail-open-corretto`",
                Watch:
                [
                    "`isOwner` è rimasto false (il TryParse non ha trovato il cliente): si entra nel ramo e si risponde 403, accesso negato.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
