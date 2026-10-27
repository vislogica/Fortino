using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch14.Scenarios;

// Caso 14.1: la password inviata in un tentativo di accesso finisce in chiaro nel registro.
// Si prova dalla console e l'effetto si legge nella pagina «Registro».
public sealed class SecretsInLogCase : Ch14Case
{
    public override string Slug => "secrets-in-log";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Segreti nel registro";
    public override string Summary =>
        "Dalla console mandi un tentativo di accesso con una password riconoscibile: la versione vulnerabile la scrive in chiaro nel registro, dove chiunque lo legga la vede.";
    public override string BookReference => "capitolo 14, caso 14.1";
    public override CodeLocation VulnerableCode => new("Ch14.Logging/Vulnerable/LoggingApi.cs", "segreti-nei-log-vulnerabile");
    public override CodeLocation SecureCode => new("Ch14.Logging/Secure/LoggingApi.cs", "segreti-nei-log-corretto");

    private const string Secret = "Segretissima-123";

    private static readonly string Send =
        "fetch('/login-attempt', {method:'POST', headers:{'Content-Type':'application/json'}, " +
        "body: JSON.stringify({username:'lumen', password:'" + Secret + "'})}).then(r=>r.status).then(console.log)";

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nella pagina «Registro» (`/log`) come Anna Rossi. Apri la console (F12 › Console).",
        "Manda un tentativo di accesso con una password riconoscibile: `" + Send + "`",
        "Ricarica la pagina «Registro» e cerca la riga appena scritta.",
    ];

    public override ManualTrial Trial => new(
        Username: "anna",
        StartPath: "/log",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Nel registro compare la riga «Tentativo di accesso: utente=lumen, password=" + Secret + "»: la password è lì in chiaro.",
            Proves: "La password, scritta nel messaggio di log «per indagare meglio», finisce nel registro: chiunque lo legga (un collega, un backup, un servizio di raccolta) la vede.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch14.Logging/Vulnerable/LoggingApi.cs",
                Line: "sulla riga `logger.LogInformation(\"Tentativo di accesso: ... password={Password}\", ...)`, nella regione `segreti-nei-log-vulnerabile`",
                Watch:
                [
                    "`request.Password` vale la password in chiaro, e viene passata al registro insieme al nome utente.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Nel registro compare «Tentativo di accesso per l'utente lumen.» e «Dati della richiesta: LoginAttemptRequest { Username = lumen, Password = *** }»: la password non c'è, al suo posto `***`.",
            Proves: "Si registra l'evento utile (chi ha provato ad accedere) senza mai scrivere il segreto; se serve la richiesta intera, SafeLog oscura i campi segnati `[Sensitive]`.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch14.Logging/Secure/LoggingApi.cs",
                Line: "sulla riga `string requestText = SafeLog.Describe(request);`, nella regione `segreti-nei-log-corretto`",
                Watch:
                [
                    "`requestText` contiene `Password = ***`: il valore sensibile è stato sostituito prima di scriverlo.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
