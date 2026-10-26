using System.Net.Http.Json;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch01.Scenarios;

// Caso 1.2: un cliente del portale chiama un indirizzo riservato allo staff, perché il controllo
// del ruolo è solo nell'interfaccia e non nell'endpoint.
public sealed class FunctionLevelCase : Ch01Case
{
    public override string Slug => "function-level";
    public override IReadOnlyList<string> LabUsers => ["lumen"];
    public override string Title => "Regola solo nell'interfaccia";
    public override string Summary =>
        "L'elenco dei clienti con il fido è nascosto ai clienti nell'interfaccia, ma l'endpoint non controlla il ruolo: un cliente lo chiede direttamente e lo ottiene.";
    public override string BookReference => "capitolo 1, caso 1.2";
    public override CodeLocation VulnerableCode => new("Ch01.AccessControl/Vulnerable/AccessApi.cs", "funzione-vulnerabile");
    public override CodeLocation SecureCode => new("Ch01.AccessControl/Secure/AccessApi.cs", "funzione-corretto");

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log("Ottica Lumen, un cliente del portale, chiede direttamente l'elenco dei clienti con il fido.");

        using var response = await client.GetAsync($"{Api(variant)}/customers", ct);
        var customers = response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<CustomerDto>>(ct) ?? []
            : [];
        run.Log($"Risposta del server: {(int)response.StatusCode}. Clienti restituiti: {customers.Count}.");

        if (customers.Count > 0)
            run.Succeeded($"Il cliente ha ottenuto l'elenco riservato di {customers.Count} clienti, con partita IVA e " +
                          "fido. Il controllo del ruolo era solo nell'interfaccia, non nell'endpoint.");
        else
            run.Blocked("L'accesso è stato negato (403): l'endpoint controlla il ruolo da solo.");
    }

    private sealed record CustomerDto(string Name, string VatNumber, decimal CreditLimit);
}
