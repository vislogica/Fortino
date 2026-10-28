using Fortino.Web.Auth;
using Fortino.Web.Data;
using Fortino.Web.Lab;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Chapters.Ch16;

// Capitolo 16 — Race condition.
//   caso 16.1  «Applica il buono» nel dettaglio di un ordine del portale clienti;
//   caso 16.2  «Crea la fattura» nel dettaglio di un ordine del gestionale.
// Il codice delle due versioni sta in Vulnerable/ e Secure/; qui ci sono le azioni dei pulsanti, che
// scelgono la versione dal cookie della prova (fuori da una prova, la corretta).
public sealed class Ch16Module : ILabChapterModule
{
    public int Chapter => 16;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.CouponReuseCase>();
        services.AddSingleton<ILabCase, Scenarios.DoubleInvoiceCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // 16.1 Il cliente applica un buono al suo ordine in bozza.
        app.MapPost("/portal/orders/{orderId:int}/buono", async (int orderId, [FromForm] CouponForm form,
            HttpContext http, DbConnectionFactory factory) =>
        {
            int? customerId = http.User.GetCustomerId();
            string code = (form.Code ?? "").Trim().ToUpperInvariant();
            await using var connection = await Ch16Shared.OpenAsync(factory);
            bool redeemed = Variant(http, "coupon-reuse") == CodeVariant.Vulnerable
                ? await Vulnerable.RaceApi.RedeemCouponAsync(connection, code, orderId)
                : await Secure.RaceApi.RedeemCouponAsync(connection, code, orderId);
            if (!redeemed)
                return Results.LocalRedirect($"/portal/orders/{orderId}?errore-buono={Uri.EscapeDataString($"Il buono {code} non è valido o è già stato usato.")}");
            await Ch16Shared.AddDiscountLineAsync(connection, code, orderId);
            return Results.LocalRedirect($"/portal/orders/{orderId}?buono={Uri.EscapeDataString(code)}");
        }).RequireAuthorization(Policies.Customer);

        // 16.2 L'operatore crea la fattura di un ordine. IFormCollection fa scattare la verifica del token antiforgery.
        app.MapPost("/orders/{orderId:int}/fattura", async (int orderId, IFormCollection _, HttpContext http, DbConnectionFactory factory) =>
        {
            await using var connection = await Ch16Shared.OpenAsync(factory);
            bool created = Variant(http, "double-invoice") == CodeVariant.Vulnerable
                ? await Vulnerable.RaceApi.CreateInvoiceAsync(connection, orderId)
                : await Secure.RaceApi.CreateInvoiceAsync(connection, orderId);
            return created
                ? Results.LocalRedirect($"/orders/{orderId}?fattura=creata")
                : Results.LocalRedirect($"/orders/{orderId}?errore-fattura={Uri.EscapeDataString("L'ordine ha già una fattura.")}");
        }).RequireAuthorization(Policies.Staff);
    }

    private CodeVariant Variant(HttpContext http, string caseSlug) =>
        new LabTrialState(ActiveTrial.Parse(http.Request.Cookies[ActiveTrial.CookieName])).VariantFor(Chapter, caseSlug);
}
