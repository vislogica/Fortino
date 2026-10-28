using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch07.Scenarios;

// Caso 7.4: un cliente toglie il controllo del browser e aggiunge al suo ordine una riga con quantità negativa.
public sealed class NegativeQuantityCase : Ch07Case
{
    public override string Slug => "negative-quantity";
    public override string Title => "Quantità negativa";
    public override string Summary =>
        "Entri nel portale come il cliente Ottica Lumen. Il modulo non lascia inviare quantità negative, ma il controllo è solo nel browser: lo togli e aggiungi al tuo ordine una riga con quantità negativa.";
    public override string BookReference => "capitolo 7, caso 7.4";
    public override CodeLocation VulnerableCode => new("Ch07.BusinessLogic/Vulnerable/OrderService.cs", "riga-ordine-vulnerabile");
    public override CodeLocation SecureCode => new("Ch07.BusinessLogic/Secure/OrderService.cs", "riga-ordine-corretta");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel portale come Ottica Lumen (cliente), nel tuo ordine ORD-2026-0105, in bozza. Annota il totale.",
        "Nel modulo sopra le righe scegli «Toner nero per stampante laser», scrivi -50 in «Quantità» e premi «Aggiungi all'ordine». Il browser blocca l'invio e avvisa che il valore deve essere almeno 1.",
        "Quel controllo lo fa il browser, e il browser è tuo. Tasto destro sul campo «Quantità» › «Ispeziona»: si apre F12 nella scheda «Elements» con il campo evidenziato. Fai doppio clic su `min=\"1\"`, cancellalo e premi Invio.",
        "Torna alla pagina, lascia -50 nella quantità e premi di nuovo «Aggiungi all'ordine».",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/portal/orders/5",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Compare il messaggio verde «Articolo aggiunto all'ordine» e il totale diventa -3.072,00 €: la riga con -50 toner vale come uno sconto enorme.",
            Proves: "Il controllo del browser si toglie in due clic, e il server non controlla che la quantità sia positiva. Se l'ordine viene approvato, genera un credito a favore del cliente.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch07.BusinessLogic/Vulnerable/OrderService.cs",
                Line: "sulla riga che aggiunge la riga all'ordine, nella regione `riga-ordine-vulnerabile`",
                Watch:
                [
                    "Il metodo inserisce la riga con la quantità arrivata, anche se negativa: nel totale `quantità × prezzo` diventa un valore negativo.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Compare il messaggio rosso «La quantità deve essere tra 1 e 1000.» e il totale non cambia: la quantità negativa è rifiutata dal server.",
            Proves: "La versione corretta ripete il controllo sul server, dove l'utente non arriva: la quantità deve essere maggiore di zero, qualunque cosa faccia il browser.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch07.BusinessLogic/Secure/OrderService.cs",
                Line: "sulla riga che controlla la quantità, nella regione `riga-ordine-corretta`",
                Watch:
                [
                    "Il metodo verifica che la quantità sia positiva e, se non lo è, solleva una `BusinessRuleException` prima di inserire la riga.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
