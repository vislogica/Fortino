using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch08.Scenarios;

// Caso 8.2: un cliente del portale aggiunge una riga all'ordine scegliendo lui il prezzo.
public sealed class ClientPriceCase : Ch08Case
{
    public override string Slug => "client-price";
    public override string Title => "Prezzo deciso dal cliente";
    public override string Summary =>
        "Entri nel portale come il cliente Ottica Lumen. Aggiungi al tuo ordine 50 toner chiamando l'API dalla console, e nella richiesta scrivi anche il prezzo: 1 centesimo l'uno.";
    public override string BookReference => "capitolo 8, caso 8.2";
    public override CodeLocation VulnerableCode => new("Ch08.MassAssignment/Vulnerable/OrderLineApi.cs", "riga-ordine-vulnerabile");
    public override CodeLocation SecureCode => new("Ch08.MassAssignment/Secure/OrderLineApi.cs", "riga-ordine-corretta");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel portale come Ottica Lumen (cliente), nel tuo ordine ORD-2026-0105, in bozza. Annota righe e totale.",
        "Aggiungi 50 toner (prodotto 2, a listino 64,00 €) chiamando l'API dalla console (F12 › Console), con il prezzo scritto da te: `fetch('/portal/api/orders/5/lines', {method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({productId:2, description:'Toner nero per stampante laser', quantity:50, unitPrice:0.01})}).then(r=>r.status)`.",
        "Il numero che compare è la risposta del server: 204 accettata, 400 rifiutata.",
        "Ricarica l'ordine (F5) e guarda la nuova riga e il totale.",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/portal/orders/5",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "La risposta è 204 e nell'ordine c'è una riga di 50 toner a 0,01 € l'uno: il totale sale di 50 centesimi invece di 3.200,00 €.",
            Proves: "Il server salva la riga così come l'ha mandata il cliente, prezzo compreso. Il listino non viene mai letto.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch08.MassAssignment/Vulnerable/OrderLineApi.cs",
                Line: "sulla riga `db.OrderLines.Add(line);`, nella regione `riga-ordine-vulnerabile`",
                Watch:
                [
                    "`line.UnitPrice` vale 0,01: è il prezzo scritto nel JSON, non quello del listino.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "La risposta è 400 e l'ordine non cambia: description e unitPrice non sono campi ammessi nella richiesta.",
            Proves: "Nella versione corretta il cliente sceglie solo prodotto e quantità; descrizione e prezzo li prende il server dal listino.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch08.MassAssignment/Secure/OrderLineApi.cs",
                Line: "sulla riga che costruisce la nuova `OrderLine`, nella regione `riga-ordine-corretta`",
                Watch:
                [
                    "Con description e unitPrice nel JSON il punto di interruzione non scatta: la richiesta è rifiutata prima, quando il JSON diventa un `AddOrderLineRequest`.",
                    "Togli i due campi dalla riga della console e riprova: ora si ferma, e `product.UnitPrice` vale 64,00, il prezzo di listino.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
