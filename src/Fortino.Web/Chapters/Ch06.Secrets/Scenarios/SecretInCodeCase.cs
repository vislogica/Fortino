using System.Net;
using System.Net.Http.Json;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch06.Scenarios;

// Caso 6.1: la chiave di firma è nel codice sorgente, quindi un attaccante che lo legge può
// firmare token validi da sé.
public sealed class SecretInCodeCase : Ch06Case
{
    public override string Slug => "secret-in-code";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Segreto nel codice";
    public override string Summary =>
        "La chiave di firma è scritta nel codice: chi legge il repository la conosce e firma token validi. La versione corretta la legge dalla configurazione.";
    public override string BookReference => "capitolo 6, caso 6.1";
    public override CodeLocation VulnerableCode => new("Ch06.Secrets/Vulnerable/SecretApi.cs", "segreto-nel-codice-vulnerabile");
    public override CodeLocation SecureCode => new("Ch06.Secrets/Secure/SecretApi.cs", "segreto-da-configurazione");

    // La chiave che l'attaccante legge nel codice sorgente (è la costante della versione vulnerabile).
    private const string KeyReadFromSource = "k3y-firma-token-fortino-2026";

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        // L'attaccante costruisce un token che lo dichiara amministratore, firmandolo con la chiave
        // trovata nel codice sorgente.
        const string payload = "utente=lumen;ruolo=admin";
        var signature = Ch06Shared.Sign(payload, KeyReadFromSource);
        run.Log($"L'attaccante legge la chiave di firma nel codice sorgente e firma un token: \"{payload}\".");

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        using var response = await client.PostAsJsonAsync($"{Api(variant)}/verify-token",
            new { Payload = payload, Signature = signature }, ct);
        run.Log($"Risposta del server al token firmato dall'attaccante: {(int)response.StatusCode} {response.ReasonPhrase}.");

        if (response.StatusCode == HttpStatusCode.OK)
            run.Succeeded("Il server ha accettato il token forgiato: la chiave era nel codice, quindi l'attaccante " +
                          "ha potuto firmarlo da sé. Chiunque legga il repository può farlo.");
        else
            run.Blocked("Il token forgiato è stato rifiutato: la chiave vera sta nella configurazione, non nel codice, " +
                        "e l'attaccante non la conosce.");
    }
}
