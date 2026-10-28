using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch07.Scenarios;

// Caso 7.1: una fattura già emessa viene modificata dalla pagina di modifica, aperta scrivendo l'indirizzo a mano.
public sealed class IssuedInvoiceCase : Ch07Case
{
    public override string Slug => "issued-invoice";
    public override string Title => "Fattura emessa modificabile";
    public override string Summary =>
        "Entri come l'operatore Mario Bianchi. L'interfaccia propone la modifica solo per le fatture in bozza, ma scrivendo a mano l'indirizzo della pagina di modifica cambi una fattura già emessa.";
    public override string BookReference => "capitolo 7, caso 7.1";
    public override CodeLocation VulnerableCode => new("Ch07.BusinessLogic/Vulnerable/InvoiceService.cs", "fattura-modifica-vulnerabile");
    public override CodeLocation SecureCode => new("Ch07.BusinessLogic/Secure/InvoiceService.cs", "fattura-modifica-corretta");

    // I passi sono gli stessi nelle due versioni: cambia solo se il server accetta o rifiuta la modifica.
    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Mario Bianchi (Operatore), nella pagina «Fatture». Annota il totale della fattura 2026/015 di Ottica Lumen: è già emessa.",
        "Guarda l'ultima colonna: il collegamento «Modifica» c'è solo sulla fattura in bozza di Panificio Sole. Le fatture emesse non si modificano, e l'interfaccia non te lo propone.",
        "Clicca «Modifica» sulla bozza e guarda la barra degli indirizzi: l'indirizzo finisce con `/invoices/3/modifica`. Il 3 è il numero interno della fattura.",
        "Nella barra degli indirizzi cambia il 3 in 2 (la fattura 2026/015, già emessa) e premi Invio: si apre la stessa pagina di modifica, per una fattura emessa.",
        "Nella riga delle penne scrivi 1,85 come prezzo e premi «Salva la riga».",
    ];

    public override ManualTrial Trial => new(
        Username: "mario",
        StartPath: "/invoices",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Compare il messaggio verde «Riga salvata» e il totale della fattura 2026/015 scende da 91,80 € a 58,50 €: una fattura già consegnata al cliente è cambiata.",
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
            Expected: "Compare il messaggio rosso «La fattura è già emessa: non si modifica, si corregge con una nota di credito.» e il totale resta 91,80 €.",
            Proves: "La versione corretta controlla che la fattura sia ancora una bozza prima di modificarla: una fattura emessa è congelata.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch07.BusinessLogic/Secure/InvoiceService.cs",
                Line: "sulla riga che controlla lo stato della fattura, nella regione `fattura-modifica-corretta`",
                Watch:
                [
                    "Il metodo legge lo stato della fattura e, se non è una bozza, solleva una `BusinessRuleException`: la modifica viene rifiutata prima di toccare i dati.",
                ])));

    // La prova si fa a mano nelle pagine vere: non c'è la prova automatica.
    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
