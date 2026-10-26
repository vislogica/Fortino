using System.Net.Http.Json;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch07.Scenarios;

// Caso 7.2: si cambia l'IBAN aziendale e cambiano anche le fatture già emesse.
public sealed class FrozenDataCase : Ch07Case
{
    public override string Slug => "frozen-data";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Dati non congelati";
    public override string Summary =>
        "Il documento di una fattura emessa legge IBAN e cliente dai dati di oggi: chi cambia l'IBAN aziendale cambia anche le fatture vecchie.";
    public override string BookReference => "capitolo 7, caso 7.2";
    public override string Owasp => "OWASP A06:2025 Insecure Design · A08:2025 Integrity Failures";
    public override CodeLocation VulnerableCode => new("Ch07.BusinessLogic/Vulnerable/InvoiceService.cs", "documento-vulnerabile");
    public override CodeLocation SecureCode => new("Ch07.BusinessLogic/Secure/InvoiceService.cs", "documento-corretto");

    private const int InvoiceId = 1;                       // fattura 2026/014, emessa il 10/10
    private const string NewIban = "IT00X0000000000000000000000";   // IBAN fittizio

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);
        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log("Utente: Anna Rossi (Amministratore). Immagina che le sue credenziali siano state rubate.");

        var before = await client.GetFromJsonAsync<InvoiceDocument>($"{Api(variant)}/invoices/{InvoiceId}/document", ct);
        run.Log($"Documento della fattura {before!.Number} del {before.IssueDate:dd/MM}: IBAN per il pagamento {before.PaymentIban}.");

        using var change = await client.PutAsJsonAsync("/lab/ch07/settings/payment-iban", new ChangePaymentIbanRequest(NewIban), ct);
        run.Log($"IBAN aziendale cambiato nelle impostazioni in {NewIban} (risposta {(int)change.StatusCode}).");

        var after = await client.GetFromJsonAsync<InvoiceDocument>($"{Api(variant)}/invoices/{InvoiceId}/document", ct);
        run.Log($"Documento della fattura {after!.Number} riscaricato: IBAN per il pagamento {after.PaymentIban}.");

        if (after.PaymentIban != before.PaymentIban)
            run.Succeeded($"La fattura emessa il {before.IssueDate:dd/MM} ora riporta il nuovo IBAN. Chi la riscarica paga sul conto sbagliato.");
        else
            run.Blocked("La fattura riporta ancora l'IBAN in vigore al momento dell'emissione. Il nuovo IBAN vale solo per le fatture future.");
    }
}
