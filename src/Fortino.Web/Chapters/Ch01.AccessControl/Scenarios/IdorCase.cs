using System.Net.Http.Json;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch01.Scenarios;

// Caso 1.1: un cliente legge l'ordine di un altro cambiando il numero nell'indirizzo (IDOR).
public sealed class IdorCase : Ch01Case
{
    public override string Slug => "idor";
    public override IReadOnlyList<string> LabUsers => ["lumen"];
    public override string Title => "Ordine di un altro cliente";
    public override string Summary =>
        "Ottica Lumen cambia il numero dell'ordine nell'indirizzo e legge un ordine di Studio Verdi. La versione corretta lega l'ordine al cliente collegato.";
    public override string BookReference => "capitolo 1, caso 1.1";
    public override CodeLocation VulnerableCode => new("Ch01.AccessControl/Vulnerable/AccessApi.cs", "idor-vulnerabile");
    public override CodeLocation SecureCode => new("Ch01.AccessControl/Secure/AccessApi.cs", "idor-corretto");

    private const int ForeignOrderId = 1;        // ORD-2026-0101, di Studio Verdi
    private const string ForeignNumber = "ORD-2026-0101";

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log($"Ottica Lumen chiede l'ordine numero {ForeignOrderId} nell'indirizzo, che è di Studio Verdi.");

        using var response = await client.GetAsync($"{Api(variant)}/orders/{ForeignOrderId}", ct);
        var order = response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<OrderDto>(ct) : null;
        run.Log($"Risposta del server: {(int)response.StatusCode}. " +
                (order is null ? "Nessun ordine restituito." : $"Ordine restituito: {order.Number} di {order.CustomerName}."));

        if (order is not null && order.Number == ForeignNumber)
            run.Succeeded($"Ottica Lumen ha letto l'ordine {order.Number} di {order.CustomerName}, che non è suo. " +
                          "Bastava cambiare il numero nell'indirizzo.");
        else
            run.Blocked("L'ordine di un altro cliente non è accessibile: il server lo lega al cliente collegato.");
    }

    private sealed record OrderDto(string Number, string CustomerName, decimal Total);
}
