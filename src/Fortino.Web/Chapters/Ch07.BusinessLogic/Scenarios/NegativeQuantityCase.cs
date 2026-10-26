using System.Net.Http.Json;
using Fortino.Web.Components;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch07.Scenarios;

// Caso 7.4: un cliente aggiunge al suo ordine una riga con quantità negativa.
public sealed class NegativeQuantityCase : Ch07Case
{
    public override string Slug => "negative-quantity";
    public override IReadOnlyList<string> LabUsers => ["lumen"];
    public override string Title => "Quantità negativa";
    public override string Summary =>
        "Con l'API del portale, un cliente aggiunge al suo ordine una riga con quantità negativa: il totale diventa un credito a suo favore.";
    public override string BookReference => "capitolo 7, caso 7.4";
    public override CodeLocation VulnerableCode => new("Ch07.BusinessLogic/Vulnerable/OrderService.cs", "riga-ordine-vulnerabile");
    public override CodeLocation SecureCode => new("Ch07.BusinessLogic/Secure/OrderService.cs", "riga-ordine-corretta");

    private const int OrderId = 5;   // ORD-2026-0105 di Ottica Lumen, in bozza
    private const string TotalSql =
        "SELECT COALESCE(SUM(Quantity * UnitPrice), 0) FROM OrderLine WHERE OrderId = @OrderId;";

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);
        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log("Utente: Ottica Lumen (Cliente del portale).");

        var before = await ReadAsync<decimal>(context, TotalSql, new { OrderId });
        run.Log($"Ordine ORD-2026-0105, in bozza, totale {Labels.Money(before)}.");

        run.Log("Richiesta all'API del portale: aggiungere -50 toner a 64,00 € l'uno.");
        using var response = await client.PostAsJsonAsync(
            $"{Api(variant)}/orders/{OrderId}/lines", new AddOrderLineRequest(ProductId: 2, Quantity: -50), ct);
        await LogResponseAsync(run, response, ct);

        var after = await ReadAsync<decimal>(context, TotalSql, new { OrderId });
        run.Log($"Ordine riletto dal database: totale {Labels.Money(after)}.");

        if (after < 0)
            run.Succeeded("Il totale dell'ordine è negativo: se approvato, genera un credito a favore del cliente.");
        else
            run.Blocked("La quantità negativa è stata rifiutata. Il totale dell'ordine non è cambiato.");
    }
}
