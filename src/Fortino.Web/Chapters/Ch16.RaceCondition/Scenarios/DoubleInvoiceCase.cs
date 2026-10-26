using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch16.Scenarios;

// Caso 16.2: due richieste simultanee creano due fatture per lo stesso ordine.
public sealed class DoubleInvoiceCase : Ch16Case
{
    public override string Slug => "double-invoice";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Doppia fattura per un ordine";
    public override string Summary =>
        "Richieste simultanee di fatturazione sullo stesso ordine creano più fatture nella versione vulnerabile. La corretta ne crea una sola.";
    public override string BookReference => "capitolo 16, caso 16.2";
    public override CodeLocation VulnerableCode => new("Ch16.RaceCondition/Vulnerable/RaceApi.cs", "doppia-fattura-vulnerabile");
    public override CodeLocation SecureCode => new("Ch16.RaceCondition/Secure/RaceApi.cs", "doppia-fattura-corretto");

    private const int OrderId = 5;   // ORD-2026-0105, senza fattura
    private const int Attempts = 6;

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log($"L'ordine {OrderId} viene fatturato {Attempts} volte nello stesso istante.");

        var tasks = Enumerable.Range(0, Attempts).Select(_ =>
            client.PostAsync($"{Api(variant)}/orders/{OrderId}/invoice", null, ct));
        var responses = await Task.WhenAll(tasks);
        foreach (var r in responses) r.Dispose();

        var invoices = await ScalarAsync<long>(context,
            "SELECT COUNT(*) FROM Invoice WHERE OrderId = @OrderId;", new { OrderId });
        run.Log($"Fatture create per l'ordine {OrderId}: {invoices}.");

        if (invoices > 1)
            run.Succeeded($"Per lo stesso ordine sono state create {invoices} fatture. In contabilità " +
                          "significa numeri doppi e importi contati più volte.");
        else
            run.Blocked("Per l'ordine è stata creata una sola fattura: le altre richieste hanno ricevuto un conflitto.");
    }
}
