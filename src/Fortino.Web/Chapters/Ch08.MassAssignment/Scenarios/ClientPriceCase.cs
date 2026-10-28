using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch08.Scenarios;

// Caso 8.2: un cliente del portale aggiunge una riga all'ordine scegliendo lui il prezzo.
public sealed class ClientPriceCase : Ch08Case
{
    public override string Slug => "client-price";
    public override string Title => "Prezzo deciso dal cliente";
    public override string Summary =>
        "Entri nel portale come il cliente Ottica Lumen. Aggiungi al tuo ordine 50 toner dal catalogo, e nella pagina di conferma cambi il prezzo scritto nell'indirizzo: 1 centesimo l'uno.";
    public override string BookReference => "capitolo 8, caso 8.2";
    public override CodeLocation VulnerableCode => new("Ch08.MassAssignment/Vulnerable/OrderLineApi.cs", "riga-ordine-vulnerabile");
    public override CodeLocation SecureCode => new("Ch08.MassAssignment/Secure/OrderLineApi.cs", "riga-ordine-corretta");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel portale come Ottica Lumen (cliente), nel tuo ordine ORD-2026-0105, in bozza. Annota righe e totale.",
        "Apri «Catalogo» nel menu. Nella riga del toner (64,00 €) scrivi 50 nella quantità e premi «Aggiungi all'ordine».",
        "Si apre la pagina di conferma. Guarda la barra degli indirizzi: dentro ci sono l'ordine, il prodotto, la descrizione, la quantità e anche `prezzo=64.00`.",
        "Nella barra degli indirizzi cambia `prezzo=64.00` in `prezzo=0.01` e premi Invio: la conferma ora dice 0,01 € l'uno.",
        "Premi «Conferma»: torni all'ordine. Guarda la nuova riga e il totale.",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/portal/orders/5",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Compare «Articolo aggiunto all'ordine» e nell'ordine c'è una riga di 50 toner a 0,01 € l'uno: il totale sale di 50 centesimi invece di 3.200,00 €.",
            Proves: "Il server salva la riga così come l'ha mandata il modulo, prezzo compreso. Il listino non viene mai letto.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch08.MassAssignment/Vulnerable/OrderLineApi.cs",
                Line: "sulla riga `db.OrderLines.Add(line);`, nella regione `riga-ordine-vulnerabile`",
                Watch:
                [
                    "`line.UnitPrice` vale 0,01: è il prezzo arrivato dal modulo, cioè dall'indirizzo che hai cambiato, non quello del listino.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Compare «Articolo aggiunto all'ordine», ma la riga dei 50 toner costa 64,00 € l'uno: il totale sale di 3.200,00 €. Il prezzo scritto nell'indirizzo è stato ignorato.",
            Proves: "Nella versione corretta il cliente sceglie solo prodotto e quantità; descrizione e prezzo li prende il server dal listino.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch08.MassAssignment/Secure/OrderLineApi.cs",
                Line: "sulla riga che costruisce la nuova `OrderLine`, nella regione `riga-ordine-corretta`",
                Watch:
                [
                    "`request` ha solo `ProductId` e `Quantity`: il prezzo 0,01 del modulo non ha dove finire.",
                    "`product.UnitPrice` vale 64,00, il prezzo di listino: è quello che va nella riga.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
