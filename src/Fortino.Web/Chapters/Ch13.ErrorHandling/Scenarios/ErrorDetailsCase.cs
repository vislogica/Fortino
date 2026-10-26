using System.Net.Http.Json;
using System.Text.Json;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch13.Scenarios;

// Caso 13.1: un'operazione che fallisce restituisce al client il messaggio tecnico dell'errore,
// con nomi di tabelle e colonne del database.
public sealed class ErrorDetailsCase : Ch13Case
{
    public override string Slug => "error-details";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Errore che rivela troppo";
    public override string Summary =>
        "Emettendo una fattura con un numero già usato, la versione vulnerabile restituisce il messaggio del database, che svela nomi di tabelle e colonne.";
    public override string BookReference => "capitolo 13, caso 13.1";
    public override CodeLocation VulnerableCode => new("Ch13.ErrorHandling/Vulnerable/ErrorHandlingApi.cs", "errore-dettagli-vulnerabile");
    public override CodeLocation SecureCode => new("Ch13.ErrorHandling/Secure/ErrorHandlingApi.cs", "errore-dettagli-corretto");

    // Parole che compaiono solo se il messaggio tecnico del database arriva al client.
    private static readonly string[] InternalClues = ["Invoice", "constraint", "SqliteException"];

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log("Entra Anna Rossi (amministratore) e prova a emettere una fattura con un numero già usato: 2026 numero 14.");

        using var response = await client.PostAsJsonAsync($"{Api(variant)}/invoices/issue", new { Year = 2026, Number = 14 }, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        run.Log($"Risposta del server: {(int)response.StatusCode}. Corpo della risposta lungo {body.Length} caratteri.");

        // Il campo "detail" della risposta è il testo che l'utente si vede arrivare.
        using var json = JsonDocument.Parse(body);
        var detail = json.RootElement.TryGetProperty("detail", out var detailElement) ? detailElement.GetString() ?? "" : "";
        run.Log("Ecco il messaggio d'errore che arriva all'utente (campo \"detail\" della risposta):", detail);

        var leaked = InternalClues.Where(c => body.Contains(c, StringComparison.OrdinalIgnoreCase)).ToList();
        if (leaked.Count > 0)
            run.Succeeded("Il messaggio di errore mostra dettagli interni: nel corpo compaiono " +
                          string.Join(", ", leaked.Select(c => $"\"{c}\"")) + ". " +
                          "Un attaccante impara così i nomi delle tabelle e la struttura del database.");
        else
            run.Blocked("La risposta è un messaggio generico con un riferimento per l'assistenza: nessun dettaglio interno.");
    }
}
