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

        services.AddSingleton<ILabCase, IssuedInvoiceCase>();
        services.AddSingleton<ILabCase, FrozenDataCase>();
        services.AddSingleton<ILabCase, SkippedStateCase>();
        services.AddSingleton<ILabCase, NegativeQuantityCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        foreach (var variant in Enum.GetValues<CodeVariant>())
            MapVariant(app.MapGroup(LabContext.BasePath(Chapter, variant)), variant);

        // Operazione legittima riservata agli amministratori, uguale per le due versioni.
        app.MapPut("/lab/ch07/settings/payment-iban",
                async (ChangePaymentIbanRequest body, HttpContext http, CompanySettingsService settings) =>
                {
                    await settings.ChangePaymentIbanAsync(body.Iban, http.User.Identity!.Name!);
                    return Results.NoContent();
                })
            .RequireAuthorization(p => p.RequireRole("Admin"));
    }

    private static void MapVariant(RouteGroupBuilder group, CodeVariant variant)
    {
        group.AddEndpointFilter(TranslateExceptions);

        group.MapPut("/invoices/{invoiceId:int}/lines/{lineId:int}",
                async (int invoiceId, int lineId, UpdateInvoiceLineRequest body, IServiceProvider sp) =>
                {
                    await sp.GetRequiredKeyedService<IInvoiceService>(variant).UpdateLineAsync(invoiceId, lineId, body);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.Staff);

        group.MapGet("/invoices/{invoiceId:int}/document",
                async (int invoiceId, IServiceProvider sp) =>
                    await sp.GetRequiredKeyedService<IInvoiceService>(variant).GetDocumentAsync(invoiceId)
                        is { } document ? Results.Ok(document) : Results.NotFound())
            .RequireAuthorization(Policies.Staff);

        group.MapPost("/orders/{orderId:int}/status",
                async (int orderId, ChangeOrderStatusRequest body, IServiceProvider sp) =>
                {
                    await sp.GetRequiredKeyedService<IOrderService>(variant).ChangeStatusAsync(orderId, body.Status);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.Staff);

        // Portale clienti: il cliente si legge dall'utente collegato, mai dalla richiesta.
        group.MapPost("/orders/{orderId:int}/lines",
                async (int orderId, AddOrderLineRequest body, HttpContext http, IServiceProvider sp) =>
                {
                    var customerId = http.User.GetCustomerId();
                    if (customerId is null) return Results.Forbid();
                    await sp.GetRequiredKeyedService<IOrderService>(variant).AddLineAsync(orderId, customerId.Value, body);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policies.Customer);
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
}
