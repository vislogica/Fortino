using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch07.Scenarios;

// Caso 7.4: un cliente aggiunge al suo ordine una riga con quantità negativa.
public sealed class NegativeQuantityCase : Ch07Case
{
    public override string Slug => "negative-quantity";
    public override string Title => "Quantità negativa";
    public override string Summary =>
        "Entri nel portale come il cliente Ottica Lumen. Il portale non lascia inserire quantità negative, ma chiamando l'API dalla console aggiungi al tuo ordine una riga con quantità negativa: il totale diventa un credito a tuo favore.";
    public override string BookReference => "capitolo 7, caso 7.4";
    public override CodeLocation VulnerableCode => new("Ch07.BusinessLogic/Vulnerable/OrderService.cs", "riga-ordine-vulnerabile");
    public override CodeLocation SecureCode => new("Ch07.BusinessLogic/Secure/OrderService.cs", "riga-ordine-corretta");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel portale come Ottica Lumen (cliente). Apri il tuo ordine ORD-2026-0105, in bozza, e annota il totale.",
        "Il portale non lascia inserire quantità negative. Falla lo stesso chiamando l'API dalla console (F12 › Console): `fetch('/portal/orders/5/lines', {method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({productId:2, quantity:-50})}).then(r=>r.status)`. Il prodotto 2 è il toner.",
        "Il numero che compare è la risposta del server: 204 accettata, 400 o 409 rifiutata.",
        "Ricarica l'ordine e guarda il totale.",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/portal/orders/5",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "La risposta è 204 e, dopo il ricarico, il totale dell'ordine è negativo: la riga con -50 toner vale come uno sconto enorme.",
            Proves: "Il server non controlla che la quantità sia positiva. Se l'ordine viene approvato, genera un credito a favore del cliente.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch07.BusinessLogic/Vulnerable/OrderService.cs",
                Line: "sulla riga che aggiunge la riga all'ordine, nella regione `riga-ordine-vulnerabile`",
                Watch:
                [
                    "Il metodo inserisce la riga con la quantità arrivata, anche se negativa: nel totale `quantità × prezzo` diventa un valore negativo.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "La risposta è 400 (richiesta non valida) e il totale non cambia: la quantità negativa è rifiutata.",
            Proves: "La versione corretta controlla che la quantità sia maggiore di zero prima di aggiungere la riga.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch07.BusinessLogic/Secure/OrderService.cs",
                Line: "sulla riga che controlla la quantità, nella regione `riga-ordine-corretta`",
                Watch:
                [
                    "Il metodo verifica che la quantità sia positiva e, se non lo è, solleva una `BusinessRuleException` (400) prima di inserire la riga.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
