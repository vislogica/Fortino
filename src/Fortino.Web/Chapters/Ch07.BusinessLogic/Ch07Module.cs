using Fortino.Web.Auth;
using Fortino.Web.Chapters.Ch07.Scenarios;
using Fortino.Web.Domain;
using Fortino.Web.Lab;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Chapters.Ch07;

// Capitolo 7 — Logica di business e progettazione insicura.
// Registra le due versioni dei servizi e le azioni dei moduli delle pagine vere che li usano:
// la modifica di una fattura, l'IBAN nelle impostazioni, i pulsanti di stato dell'ordine e
// l'aggiunta di un articolo dal portale clienti.
public sealed class Ch07Module : ILabChapterModule
{
    public int Chapter => 7;

    public sealed class InvoiceLineForm
    {
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public sealed class IbanForm
    {
        public string? Iban { get; set; }
    }

    public sealed class OrderStatusForm
    {
        public OrderStatus Status { get; set; }
    }

    public sealed class OrderLineForm
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public void AddServices(IServiceCollection services)
    {
        services.AddKeyedSingleton<IInvoiceService, Vulnerable.InvoiceService>(CodeVariant.Vulnerable);
        services.AddKeyedSingleton<IInvoiceService, Secure.InvoiceService>(CodeVariant.Secure);
        services.AddKeyedSingleton<IOrderService, Vulnerable.OrderService>(CodeVariant.Vulnerable);
        services.AddKeyedSingleton<IOrderService, Secure.OrderService>(CodeVariant.Secure);
        services.AddSingleton<CompanySettingsService>();

        services.AddSingleton<ILabCase, IssuedInvoiceCase>();
        services.AddSingleton<ILabCase, FrozenDataCase>();
        services.AddSingleton<ILabCase, SkippedStateCase>();
        services.AddSingleton<ILabCase, NegativeQuantityCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // Le azioni dei moduli. Dentro, il Laboratorio sceglie quale versione del codice risponde
        // (vulnerabile o corretta) leggendo il cookie della prova: fuori da una prova è sempre quella
        // corretta. Dopo l'azione si torna alla pagina, con l'esito o il messaggio d'errore nell'indirizzo.
        // [FromForm] fa scattare la verifica automatica del token antiforgery.

        // 7.1 Fattura emessa modificabile: il modulo della pagina /invoices/{numero}/modifica.
        app.MapPost("/invoices/{invoiceId:int}/righe/{lineId:int}",
                (int invoiceId, int lineId, [FromForm] InvoiceLineForm form, HttpContext http, IServiceProvider sp) =>
                    RunAndReturnAsync($"/invoices/{invoiceId}/modifica", () =>
                        Service<IInvoiceService>(http, sp, "issued-invoice")
                            .UpdateLineAsync(invoiceId, lineId, new UpdateInvoiceLineRequest(form.Quantity, form.UnitPrice))))
            .RequireAuthorization(Policies.Staff);

        // 7.2 Operazione legittima riservata agli amministratori (cambio IBAN aziendale), uguale per le
        // due versioni: il modulo della pagina /settings. La differenza sta nel documento della fattura.
        app.MapPost("/settings/iban",
                (HttpContext http, [FromForm] IbanForm form, CompanySettingsService settings) =>
                    RunAndReturnAsync("/settings", () =>
                        settings.ChangePaymentIbanAsync((form.Iban ?? "").Trim(), http.User.Identity!.Name!)))
            .RequireAuthorization(p => p.RequireRole("Admin"));

        // 7.3 Passaggio di stato: i pulsanti «Approva», «Spedisci» e «Annulla» della pagina /orders/{numero}.
        app.MapPost("/orders/{orderId:int}/stato",
                (int orderId, [FromForm] OrderStatusForm form, HttpContext http, IServiceProvider sp) =>
                    RunAndReturnAsync($"/orders/{orderId}", () =>
                        Service<IOrderService>(http, sp, "skipped-state").ChangeStatusAsync(orderId, form.Status)))
            .RequireAuthorization(Policies.Staff);

        // 7.4 Quantità negativa: il modulo «Aggiungi un articolo» della pagina /portal/orders/{numero}.
        // Il cliente si legge dall'utente collegato, mai dalla richiesta.
        app.MapPost("/portal/orders/{orderId:int}/righe",
                (int orderId, [FromForm] OrderLineForm form, HttpContext http, IServiceProvider sp) =>
                {
                    var customerId = http.User.GetCustomerId();
                    if (customerId is null) return Task.FromResult(Results.Forbid());
                    return RunAndReturnAsync($"/portal/orders/{orderId}", () =>
                        Service<IOrderService>(http, sp, "negative-quantity")
                            .AddLineAsync(orderId, customerId.Value, new AddOrderLineRequest(form.ProductId, form.Quantity)));
                })
            .RequireAuthorization(Policies.Customer);
    }

    // Sceglie la versione del codice (vulnerabile o corretta) in base al cookie della prova in corso.
    private static T Service<T>(HttpContext http, IServiceProvider sp, string caseSlug) where T : notnull
    {
        var trial = new LabTrialState(ActiveTrial.Parse(http.Request.Cookies[ActiveTrial.CookieName]));
        CodeVariant variant = trial.VariantFor(7, caseSlug);
        return sp.GetRequiredKeyedService<T>(variant);
    }

    // Esegue l'azione e torna alla pagina: "?esito=salvato" se è andata, "?errore=..." con il
    // messaggio leggibile se una regola del lavoro l'ha rifiutata.
    private static async Task<IResult> RunAndReturnAsync(string page, Func<Task> action)
    {
        try
        {
            await action();
            return Results.LocalRedirect($"{page}?esito=salvato");
        }
        catch (BusinessRuleException ex)
        {
            return Results.LocalRedirect($"{page}?errore={Uri.EscapeDataString(ex.Message)}");
        }
        catch (KeyNotFoundException)
        {
            return Results.LocalRedirect($"{page}?errore={Uri.EscapeDataString("Operazione non possibile: il documento non esiste o non è tuo.")}");
        }
    }
}
