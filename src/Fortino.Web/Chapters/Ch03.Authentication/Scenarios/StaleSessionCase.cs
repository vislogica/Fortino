using System.Net;
using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch03.Scenarios;

// Caso 3.2: un utente disattivato resta collegato, perché la sessione non viene ricontrollata.
public sealed class StaleSessionCase : Ch03Case
{
    public override string Slug => "stale-session";
    public override IReadOnlyList<string> LabUsers => ["lumen"];
    public override string Title => "Sessione che non muore";
    public override string Summary =>
        "Un utente viene disattivato mentre è collegato: la versione vulnerabile lo tiene dentro, la corretta ricontrolla a ogni richiesta e lo esclude.";
    public override string BookReference => "capitolo 3, caso 3.2";
    public override CodeLocation VulnerableCode => new("Ch03.Authentication/Vulnerable/AuthApi.cs", "sessione-vulnerabile");
    public override CodeLocation SecureCode => new("Ch03.Authentication/Secure/AuthApi.cs", "sessione-corretto");

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log("Ottica Lumen è collegata e usa normalmente l'applicazione.");

        await using (var db = await context.Services.GetRequiredService<DbConnectionFactory>().OpenAsync(ct))
            await db.ExecuteAsync("UPDATE AppUser SET IsActive = 0 WHERE Username = 'lumen';");
        run.Log("Un amministratore disattiva l'utente \"lumen\" (per esempio dopo un licenziamento).");

        run.Log("Ottica Lumen, con la stessa sessione, fa un'altra richiesta.");
        using var response = await client.GetAsync($"{Api(variant)}/session/whoami", ct);
        run.Log($"Risposta del server: {(int)response.StatusCode} {response.ReasonPhrase}.");

        if (response.StatusCode == HttpStatusCode.OK)
            run.Succeeded("L'utente disattivato è ancora dentro: la vecchia sessione continua a funzionare. " +
                          "Chi è stato escluso resta operativo finché il cookie non scade.");
        else
            run.Blocked("La richiesta è stata respinta (401): la sessione è stata ricontrollata e l'utente disattivato è fuori.");
    }
}
