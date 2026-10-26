using System.Net.Http.Json;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch14.Scenarios;

// Caso 14.1: la password inviata in un tentativo di accesso finisce in chiaro nel registro.
public sealed class SecretsInLogCase : Ch14Case
{
    public override string Slug => "secrets-in-log";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Segreti nel registro";
    public override string Summary =>
        "La versione vulnerabile registra il tentativo di accesso con la password in chiaro: chi legge il registro vede le password degli utenti.";
    public override string BookReference => "capitolo 14, caso 14.1";
    public override CodeLocation VulnerableCode => new("Ch14.Logging/Vulnerable/LoggingApi.cs", "segreti-nei-log-vulnerabile");
    public override CodeLocation SecureCode => new("Ch14.Logging/Secure/LoggingApi.cs", "segreti-nei-log-corretto");

    private const string Secret = "Segretissima-123";

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log($"Viene inviato un tentativo di accesso con una password riconoscibile: \"{Secret}\".");
        var start = DateTime.Now;
        using var response = await client.PostAsJsonAsync($"{Api(variant)}/login-attempt",
            new { Username = "lumen", Password = Secret }, ct);
        run.Log($"Risposta del server: {(int)response.StatusCode}.");

        // Le righe che questa richiesta ha scritto nel registro, così come le leggerebbe chiunque.
        // Il registro tiene le più recenti in cima: si rimettono in ordine di scrittura.
        var written = ReadLog(context)
            .Where(r => r.At >= start && r.Category.StartsWith("Fortino.Ch14.", StringComparison.Ordinal))
            .Reverse()
            .Select(r => r.Message);
        run.Log("Ecco cosa è stato scritto nel registro:", string.Join("\n", written));

        var leaked = ReadLog(context).Any(r => r.Message.Contains(Secret, StringComparison.Ordinal));
        if (leaked)
            run.Succeeded($"La password \"{Secret}\" compare nel registro. Chiunque possa leggere i log " +
                          "— un operatore, un backup, un sistema di raccolta — vede le password in chiaro.");
        else
            run.Blocked("Il registro contiene l'evento di accesso ma non la password: il segreto non è stato scritto.");
    }
}
