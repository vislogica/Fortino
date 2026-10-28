using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch16.Scenarios;

// Caso 16.1: lo stesso buono monouso applicato da due schede nello stesso momento.
public sealed class CouponReuseCase : Ch16Case
{
    public override string Slug => "coupon-reuse";
    public override IReadOnlyList<string> LabUsers => ["lumen"];
    public override string Title => "Buono sconto usato più volte";
    public override string Summary =>
        "Ottica Lumen applica lo stesso buono monouso da due schede, a pochi istanti di distanza: la versione vulnerabile lo accetta tutte e due le volte, la corretta una sola.";
    public override string BookReference => "capitolo 16, caso 16.1";
    public override CodeLocation VulnerableCode => new("Ch16.RaceCondition/Vulnerable/RaceApi.cs", "coupon-vulnerabile");
    public override CodeLocation SecureCode => new("Ch16.RaceCondition/Secure/RaceApi.cs", "coupon-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel portale come Ottica Lumen, nel tuo ordine ORD-2026-0105, in bozza. Annota il totale. Sotto le righe c'è il campo «Buono sconto».",
        "Apri lo stesso ordine in una seconda scheda: tasto destro sulla scheda del browser › «Duplica».",
        "In tutte e due le schede scrivi `BENVENUTO10` nel campo del buono: è un buono del 10% che si può usare una sola volta.",
        "Premi «Applica il buono» nella prima scheda e, entro 3 secondi, nella seconda. Nel Laboratorio la verifica del buono dura 3 secondi: la finestra è allargata apposta, per poterla aprire a mano.",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/portal/orders/5",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Tutte e due le schede dicono «Buono BENVENUTO10 applicato» e, ricaricando l'ordine, ci sono due righe «Sconto buono BENVENUTO10 (10%)»: il buono monouso è valso due volte.",
            Proves: "Il controllo «il buono è libero?» e la scrittura «ora è usato» sono due passi separati. Due richieste arrivate insieme superano tutte e due il controllo prima che una delle due scriva.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch16.RaceCondition/Vulnerable/RaceApi.cs",
                Line: "sulla riga `bool alreadyUsed = ...`, nella regione `coupon-vulnerabile`",
                Watch:
                [
                    "Il debugger si ferma due volte su questa riga, una per scheda, e tutte e due le volte `alreadyUsed` è false: nessuna delle due richieste ha ancora scritto.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "La prima scheda dice «Buono BENVENUTO10 applicato», la seconda «Il buono BENVENUTO10 non è valido o è già stato usato». Nell'ordine c'è una sola riga di sconto.",
            Proves: "Controllo e scrittura stanno nella stessa istruzione SQL, che il database esegue tutta insieme: anche con la stessa finestra di 3 secondi, solo una richiesta trova il buono libero.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch16.RaceCondition/Secure/RaceApi.cs",
                Line: "sulla riga `int changedRows = ...`, nella regione `coupon-corretto`",
                Watch:
                [
                    "`changedRows` vale 1 per la prima richiesta e 0 per la seconda: la condizione `AND IsUsed = 0` non trova più il buono libero.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
