using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch04.Scenarios;

// Caso 4.1: una pagina di Fortino non vieta di essere mostrata dentro un altro sito (clickjacking).
public sealed class ClickjackingCase : Ch04Case
{
    public override string Slug => "clickjacking";
    public override string Title => "Pagina incorniciabile (clickjacking)";
    public override string Summary =>
        "Entri come l'amministratrice Anna Rossi. Un sito esterno mostra, invisibile sotto un bottone esca, la pagina «Utenti» di Fortino, e ti fa disattivare un utente con un clic.";
    public override string BookReference => "capitolo 4, caso 4.1";
    public override CodeLocation VulnerableCode => new("Ch04.SecurityHeaders/Vulnerable/FrameHeaders.cs", "clickjacking-vulnerabile");
    public override CodeLocation SecureCode => new("Ch04.SecurityHeaders/Secure/FrameHeaders.cs", "clickjacking-corretto");

    // I passi sono gli stessi nelle due versioni: cambia solo se la cornice mostra la pagina o resta vuota.
    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel pannello di amministrazione, come Anna Rossi. Lascia aperta questa finestra: è il tuo gestionale, già collegato.",
        "In una nuova scheda del browser vai all'indirizzo del sito del truffatore: `https://localhost:5002/buono`. È un altro sito (porta 5002), non Fortino.",
        "Metti la spunta su «Mostra la cornice nascosta»: vedi comparire, sbiadita, la pagina «Utenti» di Fortino, con il bottone «Disattiva» di Mario Bianchi proprio sotto il bottone «Riscuoti il buono».",
        "Togli di nuovo la spunta, così la cornice torna invisibile, e premi «Riscuoti il buono».",
        "Torna alla prima finestra, la pagina «Utenti», e ricaricala: guarda lo stato di Mario Bianchi.",
    ];

    public override ManualTrial Trial => new(
        Username: "anna",
        StartPath: "/users",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Con la cornice visibile vedi che «Riscuoti» sta sopra «Disattiva». Dopo il clic, Mario Bianchi risulta disattivato: il tuo clic è arrivato alla pagina nascosta, non all'esca.",
            Proves: "Fortino non vieta di essere mostrato dentro un altro sito. Il clic parte dal tuo browser, con la tua sessione di amministratrice: per il server sei tu che disattivi l'utente, e lo esegue.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch04.SecurityHeaders/Vulnerable/FrameHeaders.cs",
                Line: "sulla riga del PASSO 1, `response.Headers.Remove(\"X-Frame-Options\");`",
                Watch:
                [
                    "Negli strumenti per sviluppatori (F12), scheda Rete, apri la risposta di `/users`: non ci sono né `X-Frame-Options` né `Content-Security-Policy`. Senza quei due header il browser accetta di mostrare la pagina in una cornice.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Con la spunta la cornice resta vuota: il browser si rifiuta di mostrare la pagina di Fortino dentro un altro sito. Il clic su «Riscuoti» non tocca Fortino, e Mario Bianchi resta attivo.",
            Proves: "I due header dicono al browser di non mostrare mai le pagine di Fortino dentro un altro sito. Senza la cornice, il clickjacking non ha su cosa agire.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch04.SecurityHeaders/Secure/FrameHeaders.cs",
                Line: "sulla riga del PASSO 2, `response.Headers.ContentSecurityPolicy = \"frame-ancestors 'none'\";`",
                Watch:
                [
                    "Nella scheda Rete la risposta di `/users` ora porta `X-Frame-Options: DENY` e `Content-Security-Policy: frame-ancestors 'none'`.",
                    "Nella scheda Console del sito del truffatore compare l'errore del browser che rifiuta di mostrare la cornice.",
                ])));

    // La prova si fa a mano nelle pagine vere: non c'è la prova automatica.
    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
