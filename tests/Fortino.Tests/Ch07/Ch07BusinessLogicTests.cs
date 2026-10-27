using System.Net;
using System.Net.Http.Json;
using Fortino.Web.Chapters.Ch07;
using Fortino.Web.Chapters.Ch07.Secure;
using Fortino.Web.Data;
using Fortino.Web.Domain;
using Fortino.Web.Lab;

namespace Fortino.Tests.Ch07;

// Capitolo 7 — Logica di business e progettazione insicura.
// Ogni prova rifà quello che fa il lettore dal browser: inizia la prova (così il Laboratorio sa quale
// versione del codice usare), entra con l'utente del caso e chiama l'API come farebbe dalla console.
public class Ch07BusinessLogicTests
{
    #region libro: test-casi-abuso
    // 7.1: la versione vulnerabile accetta la modifica di una fattura già emessa; la corretta la rifiuta.
    [Fact]
    public async Task Issued_invoice_is_frozen_only_in_the_secure_version()
    {
        Assert.Equal(HttpStatusCode.NoContent, await ChangeIssuedInvoiceLineAsync(CodeVariant.Vulnerable));
        Assert.Equal(HttpStatusCode.Conflict, await ChangeIssuedInvoiceLineAsync(CodeVariant.Secure));
    }

    // 7.2: nella versione vulnerabile il nuovo IBAN cambia anche una fattura già emessa; nella corretta no.
    [Fact]
    public async Task Issued_invoice_document_keeps_its_iban_only_in_the_secure_version()
    {
        Assert.True(await IbanChangesOnIssuedInvoiceAsync(CodeVariant.Vulnerable));
        Assert.False(await IbanChangesOnIssuedInvoiceAsync(CodeVariant.Secure));
    }

    // 7.3: la versione vulnerabile porta un ordine da bozza a spedito; la corretta rifiuta il salto.
    [Fact]
    public async Task Order_status_cannot_skip_steps_only_in_the_secure_version()
    {
        Assert.Equal(HttpStatusCode.NoContent, await SkipOrderStatusAsync(CodeVariant.Vulnerable));
        Assert.Equal(HttpStatusCode.Conflict, await SkipOrderStatusAsync(CodeVariant.Secure));
    }

    // 7.4: la versione vulnerabile accetta una riga con quantità negativa; la corretta la rifiuta.
    [Fact]
    public async Task Negative_quantity_is_rejected_only_in_the_secure_version()
    {
        Assert.Equal(HttpStatusCode.NoContent, await AddNegativeQuantityLineAsync(CodeVariant.Vulnerable));
        Assert.Equal(HttpStatusCode.BadRequest, await AddNegativeQuantityLineAsync(CodeVariant.Secure));
    }
    #endregion

    // Le transizioni di stato ammesse per un ordine (la tabella usata dalla versione corretta).
    [Theory]
    [InlineData(OrderStatus.Draft, OrderStatus.Approved, true)]
    [InlineData(OrderStatus.Approved, OrderStatus.Shipped, true)]
    [InlineData(OrderStatus.Draft, OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Draft, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Approved, false)]
    public void Order_workflow_allows_only_the_planned_transitions(OrderStatus from, OrderStatus to, bool allowed) =>
        Assert.Equal(allowed, OrderWorkflow.CanMove(from, to));

    // 7.1 — fattura 2 (2026/015), riga 3 (le penne): si prova a portarne il prezzo a 1,85 €.
    private static async Task<HttpStatusCode> ChangeIssuedInvoiceLineAsync(CodeVariant variant)
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsync(factory, "issued-invoice", variant);
        await LabLogin.LoginAsync(mario, "mario", SeedData.DemoPassword);

        using var response = await mario.PutAsJsonAsync("/staff/invoices/2/lines/3", new UpdateInvoiceLineRequest(2, 1.85m));
        return response.StatusCode;
    }

    // 7.2 — si legge l'IBAN del documento della fattura 1 (2026/014), si cambia l'IBAN aziendale, si
    // rilegge il documento: ritorna true se l'IBAN della fattura già emessa è cambiato.
    private static async Task<bool> IbanChangesOnIssuedInvoiceAsync(CodeVariant variant)
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsync(factory, "frozen-data", variant);
        await LabLogin.LoginAsync(anna, "anna", SeedData.DemoPassword);

        var before = await anna.GetFromJsonAsync<InvoiceDocument>("/staff/invoices/1/document");
        using var change = await anna.PutAsJsonAsync("/staff/settings/payment-iban",
            new ChangePaymentIbanRequest("IT00X0000000000000000000000"));
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);
        var after = await anna.GetFromJsonAsync<InvoiceDocument>("/staff/invoices/1/document");

        return before!.PaymentIban != after!.PaymentIban;
    }

    // 7.3 — ordine 6 (ORD-2026-0106), da bozza si prova a portarlo a «Spedito».
    private static async Task<HttpStatusCode> SkipOrderStatusAsync(CodeVariant variant)
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsync(factory, "skipped-state", variant);
        await LabLogin.LoginAsync(mario, "mario", SeedData.DemoPassword);

        using var response = await mario.PostAsJsonAsync("/staff/orders/6/status", new ChangeOrderStatusRequest(OrderStatus.Shipped));
        return response.StatusCode;
    }

    // 7.4 — ordine 5 (ORD-2026-0105) di Ottica Lumen: si prova ad aggiungere -50 toner (prodotto 2).
    private static async Task<HttpStatusCode> AddNegativeQuantityLineAsync(CodeVariant variant)
    {
        using var factory = new FortinoFactory();
        var lumen = await StartTrialAsync(factory, "negative-quantity", variant);
        await LabLogin.LoginAsync(lumen, "lumen", SeedData.DemoPassword);

        using var response = await lumen.PostAsJsonAsync("/portal/orders/5/lines", new AddOrderLineRequest(ProductId: 2, Quantity: -50));
        return response.StatusCode;
    }

    // "Inizia la prova" nella pagina del capitolo: scrive il cookie della prova e dice al Laboratorio
    // quale versione del codice far rispondere.
    private static async Task<HttpClient> StartTrialAsync(FortinoFactory factory, string caseSlug, CodeVariant variant)
    {
        var client = factory.CreateBrowserClient();
        var token = await LabLogin.GetAntiforgeryTokenAsync(client);
        using var start = await client.PostAsync("/lab/prova/inizia", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Chapter"] = "7",
            ["CaseSlug"] = caseSlug,
            ["Variant"] = variant.ToString(),
        }));
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        return client;
    }
}
