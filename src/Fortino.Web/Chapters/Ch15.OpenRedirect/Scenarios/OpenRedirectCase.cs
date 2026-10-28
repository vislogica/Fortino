using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch15.Scenarios;

// Caso 15.1: dopo l'accesso, il login di Fortino riporta l'utente all'indirizzo scritto in ReturnUrl.
// Un'email trappola porta la vittima al login vero con ReturnUrl verso il sito del truffatore.
public sealed class OpenRedirectCase : Ch15Case
{
    public override string Slug => "open-redirect";
    public override IReadOnlyList<string> LabUsers => ["lumen"];
    public override string Title => "Reindirizzamento aperto";
    public override string Summary =>
        "Un'email trappola porta Ottica Lumen al login vero di Fortino, ma con un indirizzo di ritorno verso il sito del truffatore: nella versione vulnerabile, dopo l'accesso, Fortino ce lo manda.";
    public override string BookReference => "capitolo 15, caso 15.1";
    public override CodeLocation VulnerableCode => new("Ch15.OpenRedirect/Vulnerable/RedirectApi.cs", "open-redirect-vulnerabile");
    public override CodeLocation SecureCode => new("Ch15.OpenRedirect/Secure/RedirectApi.cs", "open-redirect-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nel portale come Ottica Lumen. Ora fai il truffatore: apri una nuova scheda all'indirizzo `https://localhost:5002/pannello` (il sito del truffatore, su un'altra porta) e premi «Manda l'email trappola».",
        "Torna alla scheda di Fortino e apri «Posta finta»: c'è l'email «La tua fattura di ottobre è pronta», che sembra di Carta & Co. Guarda il link: comincia con l'indirizzo vero di Fortino, `https://localhost:5001/login`, ma dopo `returnUrl=` c'è l'indirizzo del truffatore.",
        "Clicca il link: si apre la pagina di accesso vera di Fortino. Entra come lumen (i campi sono già compilati) e premi «Accedi».",
        "Guarda dove ti porta il browser.",
    ];

    public override ManualTrial Trial => new(
        Username: "lumen",
        StartPath: "/portal",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Dopo l'accesso il browser lascia Fortino e arriva su `https://localhost:5002/sessione-scaduta`: una pagina che imita Fortino e chiede di nuovo le credenziali. Se le scrivi e premi «Accedi», la pagina mostra che il truffatore le ha ricevute.",
            Proves: "Fortino rimanda l'utente all'indirizzo di ritorno senza controllarlo. La vittima è partita da un link vero di Fortino e ha fatto un accesso vero: per questo si fida della pagina dopo.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch15.OpenRedirect/Vulnerable/RedirectApi.cs",
                Line: "sulla riga `IResult response = Results.Redirect(target);`, nella regione `open-redirect-vulnerabile`",
                Watch:
                [
                    "`target` è l'indirizzo del truffatore: arriva dal campo nascosto ReturnUrl del modulo, cioè dal link dell'email.",
                    "`Results.Redirect` manda il browser a qualunque indirizzo, anche fuori dal sito.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Dopo l'accesso resti su Fortino, nella pagina iniziale: l'indirizzo esterno scritto nel link è stato ignorato.",
            Proves: "La versione corretta accetta come indirizzo di ritorno solo una pagina interna al sito; tutto il resto diventa la pagina iniziale.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch15.OpenRedirect/Secure/RedirectApi.cs",
                Line: "sulla riga `string safeUrl = SafeReturnUrl(returnUrl);`, nella regione `open-redirect-corretto`",
                Watch:
                [
                    "`returnUrl` è l'indirizzo del truffatore, ma `safeUrl` vale `/`: non comincia con una sola barra, quindi non è interno.",
                    "`Results.LocalRedirect` rifiuterebbe comunque un indirizzo esterno.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
