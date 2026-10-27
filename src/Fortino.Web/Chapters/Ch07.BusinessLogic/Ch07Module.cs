using Fortino.Web.Auth;
using Fortino.Web.Chapters.Ch07.Scenarios;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch07;

// Capitolo 7 — Logica di business e progettazione insicura.
// Registra le due versioni dei servizi e i loro indirizzi:
//   /lab/ch07/vulnerable/...  → Vulnerable.InvoiceService, Vulnerable.OrderService
//   /lab/ch07/secure/...      → Secure.InvoiceService,     Secure.OrderService
public sealed class Ch07Module : ILabChapterModule
{
    public int Chapter => 7;

    public void AddServices(IServiceCollection services)
    {
        services.AddKeyedSingleton<IInvoiceService, Vulnerable.InvoiceService>(CodeVariant.Vulnerable);
        services.AddKeyedSingleton<IInvoiceService, Secure.InvoiceService>(CodeVariant.Secure);
        services.AddKeyedSingleton<IOrderService, Vulnerable.OrderService>(CodeVariant.Vulnerable);
        services.AddKeyedSingleton<IOrderService, Secure.OrderService>(CodeVariant.Secure);
        services.AddSingleton<CompanySettingsService>();
        services.AddSingleton<ILabStateReset, PaymentIbanReset>();

        services.AddSingleton<ILabCase, IssuedInvoiceCase>();
        services.AddSingleton<ILabCase, FrozenDataCase>();
        services.AddSingleton<ILabCase, SkippedStateCase>();
        services.AddSingleton<ILabCase, NegativeQuantityCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // Gli endpoint veri delle operazioni di business. Dentro, il Laboratorio sceglie quale versione
        // del codice risponde (vulnerabile o corretta) leggendo il cookie della prova: fuori da una
        // prova è sempre quella corretta. Il lettore chiama questi indirizzi dalla console del browser.
        var group = app.MapGroup("").AddEndpointFilter(TranslateExceptions);

        // 7.1 Fattura emessa modificabile.
        group.MapPut("/staff/invoices/{invoiceId:int}/lines/{lineId:int}",
                async (int invoiceId, int lineId, UpdateInvoiceLineRequest body, HttpContext http, IServiceProvider sp) =>
                {
                    await Service<IInvoiceService>(http, sp, "issued-invoice").UpdateLineAsync(invoiceId, lineId, body);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.Staff);

        // 7.2 Dati non congelati: il documento della fattura.
        group.MapGet("/staff/invoices/{invoiceId:int}/document",
                async (int invoiceId, HttpContext http, IServiceProvider sp) =>
                    await Service<IInvoiceService>(http, sp, "frozen-data").GetDocumentAsync(invoiceId)
                        is { } document ? Results.Ok(document) : Results.NotFound())
            .RequireAuthorization(Policies.Staff);

        // 7.2 Operazione legittima riservata agli amministratori (cambio IBAN aziendale), uguale per le due versioni.
        group.MapPut("/staff/settings/payment-iban",
                async (ChangePaymentIbanRequest body, HttpContext http, CompanySettingsService settings) =>
                {
                    await settings.ChangePaymentIbanAsync(body.Iban, http.User.Identity!.Name!);
                    return Results.NoContent();
                })
            .RequireAuthorization(p => p.RequireRole("Admin"));

        // 7.3 Salto di stato.
        group.MapPost("/staff/orders/{orderId:int}/status",
                async (int orderId, ChangeOrderStatusRequest body, HttpContext http, IServiceProvider sp) =>
                {
                    await Service<IOrderService>(http, sp, "skipped-state").ChangeStatusAsync(orderId, body.Status);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.Staff);

        // 7.4 Quantità negativa. Portale clienti: il cliente si legge dall'utente collegato, mai dalla richiesta.
        group.MapPost("/portal/orders/{orderId:int}/lines",
                async (int orderId, AddOrderLineRequest body, HttpContext http, IServiceProvider sp) =>
                {
                    var customerId = http.User.GetCustomerId();
                    if (customerId is null) return Results.Forbid();
                    await Service<IOrderService>(http, sp, "negative-quantity").AddLineAsync(orderId, customerId.Value, body);
                    return Results.NoContent();
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

    // Le regole violate diventano 400/409 con un messaggio leggibile; i "non trovato" diventano 404.
    private static async ValueTask<object?> TranslateExceptions(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (BusinessRuleException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: ex.StatusCode);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    // A ogni prova si rimette l'IBAN aziendale iniziale: dopo il caso 7.2 è cambiato, e la prova
    // successiva partirebbe da un IBAN diverso da quello stampato sulle fatture emesse.
    private sealed class PaymentIbanReset(CompanySettingsService settings) : ILabStateReset
    {
        public Task ResetAsync() => settings.RestoreInitialPaymentIbanAsync();
    }
}
