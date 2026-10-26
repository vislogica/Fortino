using System.Net.Http.Json;
using Fortino.Web.Components;
using Fortino.Web.Domain;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch07.Scenarios;

// Caso 7.3: un ordine in bozza passa direttamente a "spedito", saltando l'approvazione.
public sealed class SkippedStateCase : Ch07Case
{
    public override string Slug => "skipped-state";
    public override IReadOnlyList<string> LabUsers => ["mario"];
    public override string Title => "Salto di stato";
    public override string Summary =>
        "Un ordine in bozza passa direttamente a \"spedito\" senza essere stato approvato: il server scrive lo stato che riceve.";
    public override string BookReference => "capitolo 7, caso 7.3";
    public override CodeLocation VulnerableCode => new("Ch07.BusinessLogic/Vulnerable/OrderService.cs", "stato-ordine-vulnerabile");
    public override CodeLocation SecureCode => new("Ch07.BusinessLogic/Secure/OrderService.cs", "stato-ordine-corretto");

    private const int OrderId = 6;   // ORD-2026-0106 di Ferramenta Nord, in bozza

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);
        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log("Utente: Mario Bianchi (Operatore).");

        var before = (OrderStatus)await ReadAsync<long>(context, "SELECT Status FROM SalesOrder WHERE Id = @OrderId;", new { OrderId });
        run.Log($"Ordine ORD-2026-0106, stato: {Labels.Order(before).Text}.");

        run.Log("Richiesta diretta all'API: stato portato a \"Spedito\", saltando \"Approvato\".");
        using var response = await client.PostAsJsonAsync(
            $"{Api(variant)}/orders/{OrderId}/status", new ChangeOrderStatusRequest(OrderStatus.Shipped), ct);
        await LogResponseAsync(run, response, ct);

        var after = (OrderStatus)await ReadAsync<long>(context, "SELECT Status FROM SalesOrder WHERE Id = @OrderId;", new { OrderId });
        run.Log($"Ordine riletto dal database: stato {Labels.Order(after).Text}.");

        if (after == OrderStatus.Shipped)
            run.Succeeded("L'ordine risulta spedito senza essere mai stato approvato.");
        else
            run.Blocked("Il passaggio da bozza a spedito non è ammesso. L'ordine è ancora in bozza.");
    }
}
