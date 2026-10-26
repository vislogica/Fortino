using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch01.Scenarios;

// Caso 1.1: un cliente legge l'ordine di un altro cambiando il numero nell'indirizzo (IDOR).
public sealed class IdorCase : Ch01Case
{
    public override string Slug => "idor";
    public override string Title => "Ordine di un altro cliente";
    public override string Summary =>
        "Ottica Lumen apre un suo ordine nel portale, cambia il numero nella barra degli indirizzi e prova a leggere un ordine di Studio Verdi.";
    public override string BookReference => "capitolo 1, caso 1.1";
    public override CodeLocation VulnerableCode => new("Ch01.AccessControl/Vulnerable/OrderDetailReader.cs", "idor-vulnerabile");
    public override CodeLocation SecureCode => new("Ch01.AccessControl/Secure/OrderDetailReader.cs", "idor-corretto");

    // I passi sono gli stessi nelle due versioni: cambia solo quello che risponde il server.
    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel portale clienti, nella pagina «I miei ordini». Ci sono i due ordini di Ottica Lumen: `ORD-2026-0102` e `ORD-2026-0105`.",
        "Clicca sul numero `ORD-2026-0102`: si apre il dettaglio dell'ordine. Guarda la barra degli indirizzi: finisce con `/portal/orders/2`. Il 2 è il numero con cui il database identifica l'ordine.",
        "Nella barra degli indirizzi sostituisci il `2` finale con `1`, così l'indirizzo finisce con `/portal/orders/1`, e premi Invio. L'ordine 1 è di un altro cliente, Studio Verdi.",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/portal",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Si apre l'ordine `ORD-2026-0101` di Studio Verdi, con le righe e il totale. Non è un ordine di Ottica Lumen.",
            Proves: "Il codice ha una vulnerabilità: legge l'ordine solo per numero e non controlla a quale cliente appartiene. Basta cambiare un numero nell'indirizzo per leggere i dati di un altro cliente.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch01.AccessControl/Vulnerable/OrderDetailReader.cs",
                Line: "sulla riga del PASSO 1, `OrderDetail? order = await Ch01Shared.OrderByIdAsync(factory, orderId);`",
                Watch:
                [
                    "`orderId` vale 1: è il numero che hai scritto nella barra degli indirizzi.",
                    "`user` è Ottica Lumen, ma nessuna riga lo usa.",
                    "Dopo F10, `order.CustomerName` vale \"Studio Verdi\": il server ha letto l'ordine di un altro cliente.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Compare il messaggio «Ordine non trovato». L'ordine 1 esiste, ma non è di Ottica Lumen: il server risponde come se non esistesse.",
            Proves: "Il codice corretto impedisce di vedere l'ordine di un altro cliente: il server lega l'ordine al cliente collegato, qualunque numero si scriva nell'indirizzo.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch01.AccessControl/Secure/OrderDetailReader.cs",
                Line: "sulla riga del PASSO 1, `int? customerId = user.GetCustomerId();`",
                Watch:
                [
                    "`orderId` vale 1: è il numero che hai scritto nella barra degli indirizzi.",
                    "Dopo F10, `customerId` vale 2: è Ottica Lumen, letto dall'utente collegato e non dall'indirizzo.",
                    "Al PASSO 3 `order` resta null: l'ordine 1 non è del cliente 2.",
                ])));
}
