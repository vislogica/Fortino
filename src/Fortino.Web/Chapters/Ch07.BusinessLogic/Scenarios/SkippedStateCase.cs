using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch07.Scenarios;

// Caso 7.3: un ordine in bozza passa direttamente a "spedito", saltando l'approvazione.
public sealed class SkippedStateCase : Ch07Case
{
    public override string Slug => "skipped-state";
    public override string Title => "Salto di stato";
    public override string Summary =>
        "Entri come l'operatore Mario Bianchi. L'interfaccia fa avanzare un ordine un passo alla volta, ma chiamando l'API dalla console lo porti da bozza a «spedito» saltando l'approvazione.";
    public override string BookReference => "capitolo 7, caso 7.3";
    public override CodeLocation VulnerableCode => new("Ch07.BusinessLogic/Vulnerable/OrderService.cs", "stato-ordine-vulnerabile");
    public override CodeLocation SecureCode => new("Ch07.BusinessLogic/Secure/OrderService.cs", "stato-ordine-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Mario Bianchi (Operatore). Apri la pagina «Ordini» dal menu.",
        "Trova l'ordine ORD-2026-0106 di Ferramenta Nord: è in bozza.",
        "L'interfaccia fa avanzare lo stato un passo alla volta. Saltali chiamando l'API dalla console (F12 › Console): `fetch('/staff/orders/6/status', {method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({status:2})}).then(r=>r.status)`. Il valore 2 è «Spedito».",
        "Il numero che compare è la risposta del server: 204 accettata, 409 rifiutata.",
        "Ricarica la pagina «Ordini» e guarda lo stato di ORD-2026-0106.",
    ];

    public override ManualTrial Trial => new(
        Username: "mario",
        StartPath: "/orders",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "La risposta è 204 e, dopo il ricarico, ORD-2026-0106 risulta «Spedito» senza essere mai stato approvato.",
            Proves: "Il server scrive lo stato che riceve, senza controllare se il passaggio è ammesso: si salta l'approvazione.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch07.BusinessLogic/Vulnerable/OrderService.cs",
                Line: "sulla riga che scrive il nuovo stato, nella regione `stato-ordine-vulnerabile`",
                Watch:
                [
                    "Il metodo salva direttamente lo stato arrivato nella richiesta, senza confrontarlo con lo stato attuale: da «Bozza» si passa a «Spedito» in un colpo solo.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "La risposta è 409 e l'ordine resta «Bozza»: il passaggio da bozza a spedito non è ammesso.",
            Proves: "La versione corretta conosce i passaggi di stato consentiti e rifiuta quelli che saltano le tappe.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch07.BusinessLogic/Secure/OrderService.cs",
                Line: "sulla riga che controlla il passaggio di stato, nella regione `stato-ordine-corretto`",
                Watch:
                [
                    "Il metodo verifica che il nuovo stato segua quello attuale secondo le regole: un salto come «Bozza» → «Spedito» solleva una `BusinessRuleException` (409).",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
