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

    // Capitolo "di processo": non c'è un attacco da lanciare nell'applicazione. Si cercano i segreti
    // scritti nel codice con un comando e si imparano le abitudini per tenerli fuori (vedi i passi).
    public override bool IsProcess => true;

    public override string? ProcessCommand =>
        "git grep -n -iE \"api[_-]?key|password|secret|token|connectionstring\" -- \"*.cs\" \"*.json\"";

    public override IReadOnlyList<string> ProcessSteps =>
    [
        "Apri un terminale nella cartella del progetto Fortino.",
        "Cerca i segreti scritti nel codice con il comando qui sopra: elenca le righe dove compaiono parole come password, secret, token, chiave.",
        "Tra i risultati trovi la chiave di firma nel codice: src/Fortino.Web/Chapters/Ch06.Secrets/Vulnerable/SecretApi.cs, SigningKey = \"k3y-firma-token-fortino-2026\". Chi legge il repository la conosce e può firmare da sé token falsi.",
        "Confronta con la versione corretta, Secure/SecretApi.cs: nel codice c'è solo il nome dell'impostazione (Fortino:SigningKey), il valore vive fuori dal codice.",
        "Tieni un segreto fuori dal codice, in sviluppo, con gli user-secrets: nel progetto Fortino.Web esegui dotnet user-secrets init e poi dotnet user-secrets set \"Fortino:SigningKey\" \"una-chiave-a-caso\". Il valore finisce in un file fuori dalla cartella del progetto, non in Git.",
        "Ricorda la cronologia: un segreto già finito in Git resta nel passato anche se lo cancelli. Va considerato compromesso e ruotato (sostituito con uno nuovo), non solo rimosso.",
    ];

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
