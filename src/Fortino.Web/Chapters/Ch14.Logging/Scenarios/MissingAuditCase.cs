using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch14.Scenarios;

// Caso 14.2: un'operazione sensibile (disattivare un utente) non lascia traccia nel registro.
public sealed class MissingAuditCase : Ch14Case
{
    public override string Slug => "missing-audit";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Azione sensibile non tracciata";
    public override string Summary =>
        "Un amministratore disattiva un utente. La versione vulnerabile non registra nulla: dopo, nessuno può sapere chi l'ha fatto.";
    public override string BookReference => "capitolo 14, caso 14.2";
    public override CodeLocation VulnerableCode => new("Ch14.Logging/Vulnerable/LoggingApi.cs", "audit-mancante-vulnerabile");
    public override CodeLocation SecureCode => new("Ch14.Logging/Secure/LoggingApi.cs", "audit-mancante-corretto");

    private const int TargetUserId = 4;   // Ottica Lumen

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log($"Anna Rossi (amministratore) disattiva l'utente con id {TargetUserId} (Ottica Lumen).");
        using var response = await client.PostAsync($"{Api(variant)}/users/{TargetUserId}/deactivate", null, ct);
        run.Log($"Risposta del server: {(int)response.StatusCode}. L'utente è stato disattivato.");

        var audited = ReadLog(context).Any(r =>
            r.Message.Contains("AUDIT", StringComparison.Ordinal) &&
            r.Message.Contains(TargetUserId.ToString(), StringComparison.Ordinal));

        if (!audited)
            run.Succeeded("Nel registro non c'è nessuna traccia dell'operazione. Dopo un incidente, è " +
                          "impossibile ricostruire chi ha disattivato l'utente e quando.");
        else
            run.Blocked("Nel registro c'è una voce di audit con chi ha fatto l'operazione, su chi e quando.");
    }
}
