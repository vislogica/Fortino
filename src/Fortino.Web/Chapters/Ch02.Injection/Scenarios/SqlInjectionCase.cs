using System.Net.Http.Json;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch02.Scenarios;

// Caso 2.1: una ricerca costruita concatenando il testo restituisce tutti i clienti, anche con un
// termine che non corrisponde a nessuno.
public sealed class SqlInjectionCase : Ch02Case
{
    public override string Slug => "sql-injection";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "SQL injection nella ricerca";
    public override string Summary =>
        "Un termine di ricerca costruito ad arte aggira la condizione e restituisce tutti i clienti. La versione corretta usa un parametro.";
    public override string BookReference => "capitolo 2, caso 2.1";
    public override CodeLocation VulnerableCode => new("Ch02.Injection/Vulnerable/InjectionApi.cs", "sql-injection-vulnerabile");
    public override CodeLocation SecureCode => new("Ch02.Injection/Secure/InjectionApi.cs", "sql-injection-corretto");

    // Un termine che non corrisponde a nessun cliente, più un pezzo che rende la condizione sempre vera.
    private const string Payload = "zzz' OR '1'='1' -- ";

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log($"La ricerca clienti viene chiamata con un termine costruito ad arte: \"{Payload}\".");

        using var response = await client.GetAsync(
            $"{Api(variant)}/customers/search?term={Uri.EscapeDataString(Payload)}", ct);
        var hits = response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<CustomerDto>>(ct) ?? []
            : [];
        run.Log($"Risposta del server: {(int)response.StatusCode}. Clienti restituiti: {hits.Count}.");

        if (hits.Count > 1)
            run.Succeeded($"La ricerca ha restituito {hits.Count} clienti, pur cercando un nome inesistente: la " +
                          "condizione è stata aggirata. Lo stesso trucco può leggere altre tabelle, come quella degli utenti.");
        else
            run.Blocked("La ricerca non ha trovato nessun cliente con quel nome: il termine è stato trattato come dato.");
    }

    private sealed record CustomerDto(string Name, string VatNumber);
}
