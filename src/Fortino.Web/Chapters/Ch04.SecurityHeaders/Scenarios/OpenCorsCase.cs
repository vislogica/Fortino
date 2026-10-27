using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch04.Scenarios;

// Caso 4.2: il server permette a qualunque sito di leggere i dati dell'utente collegato (CORS aperto).
public sealed class OpenCorsCase : Ch04Case
{
    public override string Slug => "open-cors";
    public override string Title => "CORS troppo aperto";
    public override string Summary =>
        "Entri come Ottica Lumen. Un sito esterno prova a leggere, dal tuo browser, il riepilogo del tuo portale: con il CORS aperto ci riesce.";
    public override string BookReference => "capitolo 4, caso 4.2";
    public override CodeLocation VulnerableCode => new("Ch04.SecurityHeaders/Vulnerable/CorsHeaders.cs", "cors-vulnerabile");
    public override CodeLocation SecureCode => new("Ch04.SecurityHeaders/Secure/CorsHeaders.cs", "cors-corretto");

    // I passi sono gli stessi nelle due versioni: cambia solo se la pagina mostra i dati o l'errore.
    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel portale di Ottica Lumen. Lascia aperta questa finestra: sei collegato, e il tuo browser ha il cookie di sessione di Fortino.",
        "In una nuova scheda del browser vai all'indirizzo del sito del truffatore: `https://localhost:5002/conto`. È un altro sito (porta 5002), non Fortino.",
        "La pagina del truffatore prova subito a leggere il riepilogo del tuo portale (fido e fatture), usando il cookie che hai nel browser. Guarda cosa compare nel riquadro grigio.",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/portal",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Nel riquadro compaiono i tuoi dati: nome, partita IVA, fido e l'elenco delle fatture. Il sito del truffatore li ha letti dal tuo browser.",
            Proves: "Il server dà il permesso a qualunque sito e lascia inviare i cookie. Il JavaScript del truffatore legge i dati riservati di Ottica Lumen come se fosse il portale stesso.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch04.SecurityHeaders/Vulnerable/CorsHeaders.cs",
                Line: "sulla riga del PASSO 2, `http.Response.Headers.AccessControlAllowOrigin = origin;`",
                Watch:
                [
                    "`origin` è `https://localhost:5002`, il sito del truffatore: il codice gli restituisce quello stesso indirizzo come permesso.",
                    "Negli strumenti per sviluppatori (F12), scheda Rete, la risposta di `/portal/api/situazione` porta `Access-Control-Allow-Origin: https://localhost:5002`.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Nel riquadro compare un messaggio di errore: il browser ha bloccato la lettura. Il sito del truffatore non vede nessun dato.",
            Proves: "Il server dà il permesso solo al sito ammesso (www.cartaeco.example). A ogni altro sito non risponde con il permesso, e il browser non lascia leggere i dati.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch04.SecurityHeaders/Secure/CorsHeaders.cs",
                Line: "sulla riga del PASSO 2, `bool originIsAllowed = origin == AllowedOrigin;`",
                Watch:
                [
                    "`origin` è `https://localhost:5002` e `AllowedOrigin` è `https://www.cartaeco.example`: `originIsAllowed` vale false, quindi non si aggiunge nessun permesso.",
                    "Nella scheda Console del sito del truffatore compare l'errore CORS del browser.",
                ])));

    // La prova si fa a mano nelle pagine vere: non c'è la prova automatica.
    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
