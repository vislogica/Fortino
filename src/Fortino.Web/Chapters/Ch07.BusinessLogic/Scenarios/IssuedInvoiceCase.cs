using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch07.Scenarios;

// Caso 7.1: una fattura già emessa viene modificata chiamando direttamente l'API.
public sealed class IssuedInvoiceCase : Ch07Case
{
    public override string Slug => "issued-invoice";
    public override string Title => "Fattura emessa modificabile";
    public override string Summary =>
        "Entri come l'operatore Mario Bianchi. L'interfaccia non lascia toccare una fattura già emessa, ma chiamando l'API dalla console del browser il server accetta la modifica lo stesso.";
    public override string BookReference => "capitolo 7, caso 7.1";
    public override CodeLocation VulnerableCode => new("Ch07.BusinessLogic/Vulnerable/InvoiceService.cs", "fattura-modifica-vulnerabile");
    public override CodeLocation SecureCode => new("Ch07.BusinessLogic/Secure/InvoiceService.cs", "fattura-modifica-corretta");

    // I passi sono gli stessi nelle due versioni: cambia solo se il server accetta o rifiuta la modifica.
    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Mario Bianchi (Operatore). Apri la pagina «Fatture» dal menu.",
        "Trova la fattura 2026/015 di Ottica Lumen: è già emessa. Annota il suo totale.",
        "L'interfaccia non lascia modificare una fattura emessa. Prova a farlo lo stesso chiamando l'API: premi F12, scheda «Console», incolla questa riga e invia: `fetch('/staff/invoices/2/lines/3', {method:'PUT', headers:{'Content-Type':'application/json'}, body: JSON.stringify({quantity:2, unitPrice:1.85})}).then(r => r.status)`.",
        "Il numero che compare è la risposta del server: 204 vuol dire accettata, 409 rifiutata.",
        "Ricarica la pagina «Fatture» e guarda di nuovo il totale della fattura 2026/015.",
    ];

    public override ManualTrial Trial => new(
        Username: "mario",
        StartPath: "/invoices",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "La risposta è 204 e, dopo il ricarico, il totale della fattura 2026/015 è calato: la riga delle penne ora costa molto meno.",
            Proves: "Il server accetta la modifica di una fattura già emessa, perché non controlla lo stato del documento. Il documento consegnato al cliente non corrisponde più ai dati.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch07.BusinessLogic/Vulnerable/InvoiceService.cs",
                Line: "sulla riga che aggiorna la riga della fattura, nella regione `fattura-modifica-vulnerabile`",
                Watch:
                [
                    "Il metodo scrive la nuova quantità e il nuovo prezzo senza prima controllare lo stato della fattura: una fattura «Emessa» viene modificata come se fosse ancora una bozza.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "La risposta è 409 (conflitto) e il totale non cambia: la fattura resta identica a quella emessa.",
            Proves: "La versione corretta controlla che la fattura sia ancora una bozza prima di modificarla: una fattura emessa è congelata.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch07.BusinessLogic/Secure/InvoiceService.cs",
                Line: "sulla riga che controlla lo stato della fattura, nella regione `fattura-modifica-corretta`",
                Watch:
                [
                    "Il metodo legge lo stato della fattura e, se non è una bozza, solleva una `BusinessRuleException`: la richiesta viene rifiutata con 409 prima di toccare i dati.",
                ])));

    // La prova si fa a mano nelle pagine vere: non c'è la prova automatica.
    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
