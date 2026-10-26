using System.Net;
using System.Net.Http.Json;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch03.Scenarios;

// Caso 3.1: si provano molte password senza che l'applicazione blocchi i tentativi.
public sealed class BruteForceCase : Ch03Case
{
    public override string Slug => "brute-force";
    public override string Title => "Password provate a tappeto";
    public override string Summary =>
        "Si inviano molti tentativi di accesso con password diverse: la versione vulnerabile li elabora tutti, la corretta blocca dopo alcuni errori.";
    public override string BookReference => "capitolo 3, caso 3.1";
    public override CodeLocation VulnerableCode => new("Ch03.Authentication/Vulnerable/AuthApi.cs", "forza-bruta-vulnerabile");
    public override CodeLocation SecureCode => new("Ch03.Authentication/Secure/AuthApi.cs", "forza-bruta-corretto");

    private const int Attempts = 20;

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(null, ct);   // un attaccante non collegato
        run.Log($"Vengono provate {Attempts} password diverse per l'utente \"lumen\", una dopo l'altra.");

        var blocked = 0;
        for (var i = 0; i < Attempts; i++)
        {
            using var response = await client.PostAsJsonAsync($"{Api(variant)}/login-check",
                new { Username = "lumen", Password = $"tentativo-{i}" }, ct);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) blocked++;
        }
        run.Log($"Tentativi bloccati dal server: {blocked} su {Attempts}.");

        if (blocked == 0)
            run.Succeeded($"Tutti i {Attempts} tentativi sono stati elaborati, senza alcun blocco. Con abbastanza " +
                          "tentativi, una password debole viene trovata.");
        else
            run.Blocked($"Dopo pochi errori il server ha iniziato a bloccare: {blocked} tentativi su {Attempts} " +
                        "respinti. La prova a tappeto diventa impraticabile.");
    }
}
