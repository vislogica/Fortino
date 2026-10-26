using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch01.Scenarios;

// Caso 1.2: un cliente del portale apre una pagina riservata allo staff scrivendone l'indirizzo,
// perché la regola è solo nel menu e non nel codice che legge i dati.
public sealed class FunctionLevelCase : Ch01Case
{
    public override string Slug => "function-level";
    public override string Title => "Regola solo nell'interfaccia";
    public override string Summary =>
        "Il menu non mostra ai clienti la voce «Clienti», riservata allo staff. Ottica Lumen ne scrive l'indirizzo nella barra e prova ad aprire l'elenco di tutti i clienti.";
    public override string BookReference => "capitolo 1, caso 1.2";
    public override CodeLocation VulnerableCode => new("Ch01.AccessControl/Vulnerable/CustomerListReader.cs", "funzione-vulnerabile");
    public override CodeLocation SecureCode => new("Ch01.AccessControl/Secure/CustomerListReader.cs", "funzione-corretto");

    // I passi sono gli stessi nelle due versioni: cambia solo quello che risponde il server.
    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel portale clienti. Guarda il menu a sinistra: non c'è la voce «Clienti», che lo staff di Carta & Co. usa per vedere tutti i clienti.",
        "Apri «La mia anagrafica» dal menu: vedi solo i dati di Ottica Lumen. La barra degli indirizzi finisce con `/portal/profile`.",
        "Nella barra degli indirizzi sostituisci `/portal/profile` con `/customers`, l'indirizzo dell'elenco clienti dello staff, e premi Invio.",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/portal",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Si apre l'elenco di tutti i clienti di Carta & Co., con partita IVA, IBAN e fido. Ottica Lumen è un cliente e non dovrebbe vederlo.",
            Proves: "La regola «solo lo staff vede i clienti» era solo nell'interfaccia, che nasconde la voce del menu. Il codice che legge i dati non controlla il ruolo: chi conosce l'indirizzo entra.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch01.AccessControl/Vulnerable/CustomerListReader.cs",
                Line: "sulla riga del PASSO 1, `IReadOnlyList<CustomerRow> customers = await Ch01Shared.AllCustomersAsync(factory);`",
                Watch:
                [
                    "`user` è Ottica Lumen, con ruolo Customer, ma nessuna riga lo guarda.",
                    "Dopo F10, `customers.Count` vale 4: tutti i clienti, con i loro dati riservati.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Compare il messaggio «Accesso negato» e nessun cliente.",
            Proves: "Il codice corretto controlla il ruolo sul server, a ogni richiesta. Nascondere la voce del menu resta una comodità: anche scrivendo l'indirizzo a mano, un cliente non vede l'elenco.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch01.AccessControl/Secure/CustomerListReader.cs",
                Line: "sulla riga del PASSO 1, `bool isAdmin = user.IsInRole(\"Admin\");`",
                Watch:
                [
                    "Avanzando con F10, `isAdmin`, `isOperator` e `isStaff` valgono tutti false: Ottica Lumen è un cliente.",
                    "Al PASSO 2 il codice entra nell'if e restituisce `denied`: l'elenco non viene nemmeno letto dal database.",
                ])));
}
