using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch14.Scenarios;

// Caso 14.1: la password inviata nel login finisce in chiaro nel registro. Si prova con la
// maschera di login vera: l'accesso che fai per iniziare la prova viene registrato, e nella pagina
// «Registro» si vede (o no) la password.
public sealed class SecretsInLogCase : Ch14Case
{
    public override string Slug => "secrets-in-log";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Segreti nel registro";
    public override string Summary =>
        "Fai l'accesso dalla maschera di login vera con la tua password: la versione vulnerabile la scrive in chiaro nel registro, dove chiunque lo legga la vede.";
    public override string BookReference => "capitolo 14, caso 14.1";
    public override CodeLocation VulnerableCode => new("Ch14.Logging/Vulnerable/LoggingApi.cs", "segreti-nei-log-vulnerabile");
    public override CodeLocation SecureCode => new("Ch14.Logging/Secure/LoggingApi.cs", "segreti-nei-log-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Premendo «Inizia la prova» hai appena fatto l'accesso dalla maschera di login vera, con l'utente `anna` e la sua password. Ora sei nella pagina «Registro» (`/log`).",
        "Guarda in cima al registro la riga del tentativo di accesso scritta dal tuo login. Se non c'è, premi «Aggiorna».",
    ];

    public override ManualTrial Trial => new(
        Username: "anna",
        StartPath: "/log",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "C'è la riga «Tentativo di accesso: utente=anna, password=…»: la password che hai scritto per entrare è nel registro in chiaro.",
            Proves: "La password del login, scritta nel messaggio «per indagare meglio sugli accessi», finisce nel registro: chiunque lo legga (un collega, un backup, un servizio di raccolta) la vede.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch14.Logging/Vulnerable/LoggingApi.cs",
                Line: "sulla riga `logger.LogInformation(\"Tentativo di accesso: ... password={Password}\", ...)`, nella regione `segreti-nei-log-vulnerabile`",
                Watch:
                [
                    "`password` è la password che hai scritto nel login, e viene passata al registro insieme al nome utente.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "C'è «Tentativo di accesso per l'utente anna.» e «Dati della richiesta: LoginAttemptRequest { Username = anna, Password = *** }»: la password non c'è, al suo posto `***`.",
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
