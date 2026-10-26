using System.Net.Http.Json;
using Fortino.Web.Components;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch07.Scenarios;

// Caso 7.1: una fattura già emessa viene modificata chiamando direttamente l'API.
public sealed class IssuedInvoiceCase : Ch07Case
{
    public override string Slug => "issued-invoice";
    public override IReadOnlyList<string> LabUsers => ["mario"];
    public override string Title => "Fattura emessa modificabile";
    public override string Summary =>
        "Il server accetta la modifica di una riga anche su una fattura già emessa: basta chiamare direttamente l'API.";
    public override string BookReference => "capitolo 7, caso 7.1";
    public override CodeLocation VulnerableCode => new("Ch07.BusinessLogic/Vulnerable/InvoiceService.cs", "fattura-modifica-vulnerabile");
    public override CodeLocation SecureCode => new("Ch07.BusinessLogic/Secure/InvoiceService.cs", "fattura-modifica-corretta");

    private const int InvoiceId = 2;   // fattura 2026/015 di Ottica Lumen, emessa il 14/10

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);
        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log("Utente: Mario Bianchi (Operatore).");

        var before = await ReadAsync<decimal>(context, "SELECT Total FROM Invoice WHERE Id = @InvoiceId;", new { InvoiceId });
        var lineId = await ReadAsync<long>(context, "SELECT MIN(Id) FROM InvoiceLine WHERE InvoiceId = @InvoiceId;", new { InvoiceId });
        run.Log($"Fattura 2026/015, stato: Emessa, totale {Labels.Money(before)}.");

        run.Log("Richiesta diretta all'API: prezzo delle penne portato da 18,50 € a 1,85 €.");
        using var response = await client.PutAsJsonAsync(
            $"{Api(variant)}/invoices/{InvoiceId}/lines/{lineId}", new UpdateInvoiceLineRequest(2, 1.85m), ct);
        await LogResponseAsync(run, response, ct);

        var after = await ReadAsync<decimal>(context, "SELECT Total FROM Invoice WHERE Id = @InvoiceId;", new { InvoiceId });
        run.Log($"Fattura 2026/015 riletta dal database: totale {Labels.Money(after)}.");

        if (after != before)
            run.Succeeded("La fattura emessa è stata modificata. Il documento consegnato al cliente non corrisponde più ai dati.");
        else
            run.Blocked("Il server ha rifiutato la modifica. La fattura è identica a quella emessa.");
    }
}
