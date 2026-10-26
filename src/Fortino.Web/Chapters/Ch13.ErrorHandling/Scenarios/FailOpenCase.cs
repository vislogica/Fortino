using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch13.Scenarios;

// Caso 13.2: il controllo del proprietario lancia un'eccezione per un utente senza cliente
// associato; la versione vulnerabile, invece di negare, lascia passare (fail open).
public sealed class FailOpenCase : Ch13Case
{
    public override string Slug => "fail-open";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Errore che apre la porta";
    public override string Summary =>
        "Un amministratore, che non ha un cliente associato, apre la nota riservata di un ordine altrui: il controllo del proprietario va in errore e la versione vulnerabile concede l'accesso.";
    public override string BookReference => "capitolo 13, caso 13.2";
    public override CodeLocation VulnerableCode => new("Ch13.ErrorHandling/Vulnerable/ErrorHandlingApi.cs", "fail-open-vulnerabile");
    public override CodeLocation SecureCode => new("Ch13.ErrorHandling/Secure/ErrorHandlingApi.cs", "fail-open-corretto");

    private const int ForeignOrderId = 1;   // ORD-2026-0101, di Studio Verdi (cliente 1)

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log("Entra Anna Rossi (amministratore): il suo profilo non ha un cliente associato, quindi il " +
                "controllo del proprietario — che legge l'identificativo del cliente — va in errore.");

        run.Log($"Chiede la nota riservata dell'ordine {ForeignOrderId}, che è di Studio Verdi.");
        using var response = await client.GetAsync($"{Api(variant)}/orders/{ForeignOrderId}/note", ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        run.Log($"Risposta del server: {(int)response.StatusCode} {response.ReasonPhrase}.");

        if (response.IsSuccessStatusCode && body.Contains("Nota riservata"))
            run.Succeeded("L'accesso è stato concesso: il controllo è andato in errore e la versione vulnerabile " +
                          "ha lasciato passare. Anna legge la nota riservata di un ordine che non è suo.");
        else
            run.Blocked("L'accesso è stato negato (403): in caso di errore nel controllo, la versione corretta nega.");
    }
}
