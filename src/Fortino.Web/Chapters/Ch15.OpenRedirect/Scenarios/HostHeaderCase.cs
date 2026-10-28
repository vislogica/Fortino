using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch15.Scenarios;

// Caso 15.2: il link di reset si costruisce con il nome host preso dalla richiesta, che un
// attaccante falsifica: il link arriva in un'email autentica ma punta al suo server.
public sealed class HostHeaderCase : Ch15Case
{
    public override string Slug => "host-header";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Link avvelenato dall'Host";
    public override string Summary =>
        "Il truffatore chiede il reset della password di Anna dichiarando come nome del sito il suo indirizzo: nella versione vulnerabile l'email vera di Fortino contiene un link verso il suo server.";
    public override string BookReference => "capitolo 15, caso 15.2";
    public override CodeLocation VulnerableCode => new("Ch15.OpenRedirect/Vulnerable/RedirectApi.cs", "host-header-vulnerabile");
    public override CodeLocation SecureCode => new("Ch15.OpenRedirect/Secure/RedirectApi.cs", "host-header-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel gestionale come Anna Rossi. Ora fai il truffatore: apri una nuova scheda all'indirizzo `https://localhost:5002/pannello` (il sito del truffatore, su un'altra porta).",
        "Nel riquadro «Caso 15.2» lascia `anna` e premi «Chiedi il reset per la vittima». Il pannello manda a Fortino la richiesta che parte da «Password dimenticata?», ma dichiara come nome del sito il proprio indirizzo (intestazione `X-Forwarded-Host`). Un truffatore vero lo fa con un suo strumento; qui lo fa il pannello.",
        "Ora sei la vittima: torna alla scheda di Fortino e apri «Posta finta». C'è un'email vera di Fortino, «Reimposta la password». Guarda il link e cliccalo.",
    ];

    public override ManualTrial Trial => new(
        Username: "anna",
        StartPath: "/mailbox",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Il link dell'email comincia con `https://localhost:5002/reset-password`: punta al sito del truffatore. Cliccandolo arrivi sulla sua pagina, che mostra il codice di reset appena ricevuto.",
            Proves: "Fortino costruisce il link con il nome del sito che arriva nella richiesta. Il truffatore lo sceglie, e l'email vera di Fortino consegna il codice di reset a lui.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch15.OpenRedirect/Vulnerable/RedirectApi.cs",
                Line: "sulla riga `string host = forwardedHost ?? http.Host.Value!;`, nella regione `host-header-vulnerabile`",
                Watch:
                [
                    "`forwardedHost` vale `localhost:5002`: l'ha scritto il pannello del truffatore nell'intestazione X-Forwarded-Host.",
                    "`link` comincia con l'indirizzo del truffatore: è quello che finisce nell'email.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Il link dell'email comincia con l'indirizzo vero di Fortino, `https://localhost:5001/reset-password`. Cliccandolo arrivi sulla pagina di Fortino: il nome del sito dichiarato dal truffatore è stato ignorato.",
            Proves: "La versione corretta costruisce il link con l'indirizzo ufficiale, letto dalla configurazione: niente di quello che arriva nella richiesta finisce nel link.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch15.OpenRedirect/Secure/RedirectApi.cs",
                Line: "sulla riga `string officialUrl = config[\"App:PublicUrl\"]!;`, nella regione `host-header-corretto`",
                Watch:
                [
                    "`officialUrl` vale `https://localhost:5001`, dalla configurazione: l'intestazione falsa non viene nemmeno letta.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
