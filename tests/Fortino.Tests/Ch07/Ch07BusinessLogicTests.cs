using System.Net;
using System.Text.RegularExpressions;
using Fortino.Web.Chapters.Ch07.Secure;
using Fortino.Web.Data;
using Fortino.Web.Domain;
using Fortino.Web.Lab;

namespace Fortino.Tests.Ch07;

// Capitolo 7 — Logica di business e progettazione insicura.
// Ogni prova rifà quello che fa il lettore dal browser: inizia la prova (così il Laboratorio sa quale
// versione del codice usare), entra con l'utente del caso, apre la pagina vera e invia il suo modulo.
// Il server risponde rimandando alla pagina: con "esito=salvato" se ha accettato, con "errore=" se no.
public class Ch07BusinessLogicTests
{
    #region libro: test-casi-abuso
    // 7.1: la versione vulnerabile accetta la modifica di una fattura già emessa; la corretta la rifiuta.
    [Fact]
    public async Task Issued_invoice_is_frozen_only_in_the_secure_version()
    {
        Assert.Contains("esito=salvato", await ChangeIssuedInvoiceLineAsync(CodeVariant.Vulnerable));
        Assert.Contains("errore=", await ChangeIssuedInvoiceLineAsync(CodeVariant.Secure));
    }

    // 7.2: nella versione vulnerabile il nuovo IBAN cambia anche una fattura già emessa; nella corretta no.
    [Fact]
    public async Task Issued_invoice_document_keeps_its_iban_only_in_the_secure_version()
    {
        Assert.True(await IbanChangesOnIssuedInvoiceAsync(CodeVariant.Vulnerable));
        Assert.False(await IbanChangesOnIssuedInvoiceAsync(CodeVariant.Secure));
    }

    // 7.3: la versione vulnerabile spedisce un ordine già annullato; la corretta rifiuta il passaggio.
    [Fact]
    public async Task Cancelled_order_cannot_be_shipped_only_in_the_secure_version()
    {
        Assert.Contains("esito=salvato", await ShipCancelledOrderAsync(CodeVariant.Vulnerable));
        Assert.Contains("errore=", await ShipCancelledOrderAsync(CodeVariant.Secure));
    }

    // 7.4: la versione vulnerabile accetta una riga con quantità negativa; la corretta la rifiuta.
    [Fact]
    public async Task Negative_quantity_is_rejected_only_in_the_secure_version()
    {
        Assert.Contains("esito=salvato", await AddNegativeQuantityLineAsync(CodeVariant.Vulnerable));
        Assert.Contains("errore=", await AddNegativeQuantityLineAsync(CodeVariant.Secure));
    }
    #endregion

    // 7.2: una nuova prova riparte dall'IBAN iniziale, anche se la prova precedente l'ha cambiato.
    [Fact]
    public async Task New_trial_restores_the_initial_iban()
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsync(factory, "frozen-data", CodeVariant.Vulnerable);
        await LabLogin.LoginAsync(anna, "anna", SeedData.DemoPassword);
        await SubmitFormAsync(anna, "/settings", "/settings/iban", new() { ["Iban"] = NewIban });

        var next = await StartTrialAsync(factory, "frozen-data", CodeVariant.Vulnerable);
        await LabLogin.LoginAsync(next, "anna", SeedData.DemoPassword);
        var document = await next.GetStringAsync("/invoices/1");

        Assert.Contains(SeedData.PaymentIban, document);
    }

    // Le transizioni di stato ammesse per un ordine (la tabella usata dalla versione corretta).
    [Theory]
    [InlineData(OrderStatus.Draft, OrderStatus.Approved, true)]
    [InlineData(OrderStatus.Approved, OrderStatus.Shipped, true)]
    [InlineData(OrderStatus.Draft, OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Draft, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Approved, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Shipped, false)]
    public void Order_workflow_allows_only_the_planned_transitions(OrderStatus from, OrderStatus to, bool allowed) =>
        Assert.Equal(allowed, OrderWorkflow.CanMove(from, to));

    private const string NewIban = "IT00X0000000000000000000000";

    // 7.1 — fattura 2 (2026/015), riga 3 (le penne): dalla pagina di modifica aperta a mano
    // si prova a portarne il prezzo a 1,85 €. Ritorna dove rimanda il server.
    private static async Task<string> ChangeIssuedInvoiceLineAsync(CodeVariant variant)
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsync(factory, "issued-invoice", variant);
        await LabLogin.LoginAsync(mario, "mario", SeedData.DemoPassword);

        return await SubmitFormAsync(mario, "/invoices/2/modifica", "/invoices/2/righe/3",
            new() { ["Quantity"] = "2", ["UnitPrice"] = "1.85" });
    }

    // 7.2 — si legge l'IBAN sul documento della fattura 1 (2026/014), si cambia l'IBAN aziendale dalla
    // pagina «Impostazioni», si rilegge il documento: ritorna true se l'IBAN della fattura emessa è cambiato.
    private static async Task<bool> IbanChangesOnIssuedInvoiceAsync(CodeVariant variant)
    {
        using var factory = new FortinoFactory();
        var anna = await StartTrialAsync(factory, "frozen-data", variant);
        await LabLogin.LoginAsync(anna, "anna", SeedData.DemoPassword);

        Assert.Contains(SeedData.PaymentIban, await anna.GetStringAsync("/invoices/1"));
        Assert.Contains("esito=salvato", await SubmitFormAsync(anna, "/settings", "/settings/iban", new() { ["Iban"] = NewIban }));
        return (await anna.GetStringAsync("/invoices/1")).Contains(NewIban);
    }

    // 7.3 — ordine 6 (ORD-2026-0106): lo si approva, un collega lo annulla da un'altra scheda, poi dalla
    // prima scheda (rimasta a «Approvato») si preme «Spedisci». Ritorna dove rimanda il server.
    private static async Task<string> ShipCancelledOrderAsync(CodeVariant variant)
    {
        using var factory = new FortinoFactory();
        var mario = await StartTrialAsync(factory, "skipped-state", variant);
        await LabLogin.LoginAsync(mario, "mario", SeedData.DemoPassword);

        var token = await PageTokenAsync(mario, "/orders/6");
        Assert.Contains("esito=salvato", await PostFormAsync(mario, "/orders/6/stato", token, new() { ["Status"] = "Approved" }));
        Assert.Contains("esito=salvato", await PostFormAsync(mario, "/orders/6/stato", token, new() { ["Status"] = "Cancelled" }));
        return await PostFormAsync(mario, "/orders/6/stato", token, new() { ["Status"] = "Shipped" });
    }

    // 7.4 — ordine 5 (ORD-2026-0105) di Ottica Lumen: si prova ad aggiungere -50 toner (prodotto 2),
    // come dopo aver tolto min="1" dal campo. Ritorna dove rimanda il server.
    private static async Task<string> AddNegativeQuantityLineAsync(CodeVariant variant)
    {
        using var factory = new FortinoFactory();
        var lumen = await StartTrialAsync(factory, "negative-quantity", variant);
        await LabLogin.LoginAsync(lumen, "lumen", SeedData.DemoPassword);

        return await SubmitFormAsync(lumen, "/portal/orders/5", "/portal/orders/5/righe",
            new() { ["ProductId"] = "2", ["Quantity"] = "-50" });
    }

    // Apre la pagina, prende il token antiforgery del suo modulo e invia i campi all'azione.
    private static async Task<string> SubmitFormAsync(HttpClient client, string page, string action, Dictionary<string, string> fields) =>
        await PostFormAsync(client, action, await PageTokenAsync(client, page), fields);

    private static async Task<string> PageTokenAsync(HttpClient client, string page)
    {
        var html = await client.GetStringAsync(page);
        var token = Regex.Match(html, """name="__RequestVerificationToken"[^>]*?value="(?<t>[^"]+)"|value="(?<t>[^"]+)"[^>]*?name="__RequestVerificationToken" """);
        Assert.True(token.Success, $"Nessun modulo con token nella pagina {page}.");
        return WebUtility.HtmlDecode(token.Groups["t"].Value);
    }

    private static async Task<string> PostFormAsync(HttpClient client, string action, string token, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = token;
        using var response = await client.PostAsync(action, new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response.Headers.Location?.OriginalString ?? "";
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
