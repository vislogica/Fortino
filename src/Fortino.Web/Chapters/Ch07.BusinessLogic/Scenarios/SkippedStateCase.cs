using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch07.Scenarios;

// Caso 7.3: un ordine annullato passa a "spedito" da una pagina rimasta aperta, saltando le regole del ciclo di vita.
public sealed class SkippedStateCase : Ch07Case
{
    public override string Slug => "skipped-state";
    public override string Title => "Salto di stato";
    public override string Summary =>
        "Entri come l'operatore Mario Bianchi. L'interfaccia propone solo i passaggi di stato ammessi, ma una pagina rimasta aperta propone quelli di prima: con due schede spedisci un ordine già annullato.";
    public override string BookReference => "capitolo 7, caso 7.3";
    public override CodeLocation VulnerableCode => new("Ch07.BusinessLogic/Vulnerable/OrderService.cs", "stato-ordine-vulnerabile");
    public override CodeLocation SecureCode => new("Ch07.BusinessLogic/Secure/OrderService.cs", "stato-ordine-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Mario Bianchi (Operatore). Apri «Ordini» e poi l'ordine ORD-2026-0106 di Ferramenta Nord: è in bozza.",
        "Premi «Approva». L'ordine diventa «Approvato» e i pulsanti diventano «Spedisci» e «Annulla l'ordine».",
        "Apri lo stesso ordine in una seconda scheda: tasto destro sulla scheda del browser › «Duplica». Immagina che sia la scrivania di un collega.",
        "Nella seconda scheda premi «Annulla l'ordine»: l'ordine è «Annullato» e i pulsanti spariscono, perché un ordine annullato non va più avanti.",
        "Torna alla prima scheda senza ricaricarla: mostra ancora «Approvato» con il pulsante «Spedisci». Premilo.",
    ];

    public override ManualTrial Trial => new(
        Username: "mario",
        StartPath: "/orders",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Compare il messaggio verde «Stato dell'ordine aggiornato» e ORD-2026-0106 risulta «Spedito»: un ordine annullato è partito lo stesso.",
            Proves: "Il server scrive lo stato che riceve, senza controllare se il passaggio è ammesso partendo dallo stato di adesso: si fida dei pulsanti che la pagina mostrava.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch07.BusinessLogic/Vulnerable/OrderService.cs",
                Line: "sulla riga che scrive il nuovo stato, nella regione `stato-ordine-vulnerabile`",
                Watch:
                [
                    "Il metodo salva direttamente lo stato arrivato nella richiesta, senza confrontarlo con lo stato attuale: da «Annullato» si passa a «Spedito».",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Compare il messaggio rosso «Passaggio non ammesso: da \"Annullato\" a \"Spedito\".» e l'ordine resta «Annullato».",
            Proves: "La versione corretta rilegge lo stato di adesso dal database e controlla che il passaggio sia tra quelli consentiti: non si fida della pagina.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch07.BusinessLogic/Secure/OrderService.cs",
                Line: "sulla riga che controlla il passaggio di stato, nella regione `stato-ordine-corretto`",
                Watch:
                [
                    "Il metodo legge lo stato attuale («Annullato») e verifica che il nuovo stato lo possa seguire: da «Annullato» non si va da nessuna parte, quindi solleva una `BusinessRuleException`.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
