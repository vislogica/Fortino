using System.Net.Http.Json;
using Dapper;
using Fortino.Web.Components;
using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch08.Scenarios;

// Caso 8.2: un cliente del portale aggiunge una riga all'ordine scegliendo lui il prezzo.
public sealed class ClientPriceCase : Ch08Case
{
    public override string Slug => "client-price";
    public override IReadOnlyList<string> LabUsers => ["lumen"];
    public override string Title => "Prezzo deciso dal cliente";
    public override string Summary =>
        "Aggiungendo un prodotto al suo ordine, un cliente scrive nella richiesta anche il prezzo: 50 toner a 1 centesimo l'uno.";
    public override string BookReference => "capitolo 8, caso 8.2";
    public override CodeLocation VulnerableCode => new("Ch08.MassAssignment/Vulnerable/OrderLineApi.cs", "riga-ordine-vulnerabile");
    public override CodeLocation SecureCode => new("Ch08.MassAssignment/Secure/OrderLineApi.cs", "riga-ordine-corretta");

    private const int OrderId = 5;   // ORD-2026-0105 di Ottica Lumen, in bozza
    private const string LinesSql =
        """
        SELECT l.Description, l.Quantity, l.UnitPrice, p.UnitPrice AS ListPrice
          FROM OrderLine l JOIN Product p ON p.Id = l.ProductId
         WHERE l.OrderId = @OrderId ORDER BY l.Id;
        """;

    // Una riga dell'ordine con il prezzo di listino del prodotto, per confrontarli.
    private sealed record LineRow(string Description, long Quantity, decimal UnitPrice, decimal ListPrice)
    {
        public bool OffList => UnitPrice != ListPrice;

        public override string ToString() =>
            $"{Description}, {Quantity} × {Labels.Money(UnitPrice)}" + (OffList ? $" (listino {Labels.Money(ListPrice)})" : "");
    }

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);
        var before = await ReadLinesAsync(context);
        LogLines(run, "PRIMA dell'attacco", before);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log("Entra Ottica Lumen, cliente del portale.");

        var body = new { productId = 2, description = "Toner nero per stampante laser", quantity = 50, unitPrice = 0.01m };
        run.Log("POST /orders/5/lines: 50 toner, con il prezzo scritto nella richiesta. Campi inviati: " +
                "productId = 2, description = Toner nero per stampante laser, quantity = 50, unitPrice = 0.01.");
        using var response = await client.PostAsJsonAsync($"{Api(variant)}/orders/{OrderId}/lines", body, ct);
        await LogResponseAsync(run, response, ct);

        var after = await ReadLinesAsync(context);
        LogLines(run, "DOPO l'attacco", after);

        if (after.Any(l => l.OffList))
            run.Succeeded("Il cliente ha deciso il prezzo: 50 toner pagati 0,50 € invece di 3.200,00 €. " +
                          "Il database resta così: premi \"Ripristina i dati\" per tornare all'ordine originale.");
        else
            run.Blocked("Il prezzo scritto nella richiesta non è passato. L'ordine contiene solo prezzi di listino.");
    }

    private static async Task<List<LineRow>> ReadLinesAsync(LabContext context)
    {
        await using var db = await context.Services.GetRequiredService<DbConnectionFactory>().OpenAsync();
        return (await db.QueryAsync<LineRow>(LinesSql, new { OrderId })).ToList();
    }

    private static void LogLines(AttackRun run, string when, List<LineRow> lines) =>
        run.Log($"Ordine ORD-2026-0105 nel database {when}: {(lines.Count == 1 ? "1 riga" : $"{lines.Count} righe")}, totale " +
                $"{Labels.Money(lines.Sum(l => l.Quantity * l.UnitPrice))}. " +
                string.Join("; ", lines.Select((l, i) => $"riga {i + 1}: {l}")) + ".");
}
