using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch14.Scenarios;

// Caso 14.2: un'operazione sensibile (disattivare un utente) non lascia traccia nel registro.
// Si prova con la pagina Utenti vera: si preme «Disattiva» e si guarda il Registro.
public sealed class MissingAuditCase : Ch14Case
{
    public override string Slug => "missing-audit";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Azione sensibile non tracciata";
    public override string Summary =>
        "Dalla pagina Utenti disattivi un utente con il pulsante «Disattiva». La versione vulnerabile non scrive nulla nel registro: dopo, nessuno può sapere chi l'ha fatto.";
    public override string BookReference => "capitolo 14, caso 14.2";
    public override CodeLocation VulnerableCode => new("Ch14.Logging/Vulnerable/LoggingApi.cs", "audit-mancante-vulnerabile");
    public override CodeLocation SecureCode => new("Ch14.Logging/Secure/LoggingApi.cs", "audit-mancante-corretto");

    private static readonly IReadOnlyList<string> Steps =
    [
        "Dopo l'accesso sei nella pagina «Utenti» (`/users`) come Anna Rossi (amministratore).",
        "Trova Ottica Lumen nell'elenco e premi «Disattiva».",
        "Apri la pagina «Registro» (menu Strumenti › «Registro») e cerca una traccia dell'operazione.",
    ];

    public override ManualTrial Trial => new(
        Username: "anna",
        StartPath: "/users",
        Vulnerable: new TrialInstructions(
            Steps,
            Expected: "Ottica Lumen è disattivata, ma nel registro non c'è nessuna riga sull'operazione: dopo un incidente è impossibile sapere chi ha disattivato chi, e quando.",
            Proves: "L'operazione sensibile viene eseguita senza lasciare traccia: senza una voce di audit, non c'è modo di ricostruire cos'è successo.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch14.Logging/Vulnerable/LoggingApi.cs",
                Line: "dentro `LogDeactivation`, nella regione `audit-mancante-vulnerabile`",
                Watch:
                [
                    "Il metodo è vuoto: non c'è nessuna chiamata a `logger`, quindi la disattivazione non lascia traccia.",
                ])),
        Secure: new TrialInstructions(
            Steps,
            Expected: "Nel registro compare la riga «AUDIT: l'utente Anna Rossi ha disattivato l'utente con id 4.», con l'ora: chi ha agito, su chi, quando.",
            Proves: "Ogni operazione sensibile lascia una voce di audit, che permette di ricostruire cos'è successo.",
            Breakpoint: new BreakpointHint(
                File: "Chapters/Ch14.Logging/Secure/LoggingApi.cs",
                Line: "sulla riga `logger.LogInformation(\"AUDIT: ...\", actorName, userId);`, nella regione `audit-mancante-corretto`",
                Watch:
                [
                    "`actorName` è «Anna Rossi» e `userId` è 4: la voce di audit dice chi ha agito e su chi.",
                ])));

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
