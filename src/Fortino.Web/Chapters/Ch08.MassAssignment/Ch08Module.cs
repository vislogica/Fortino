using Fortino.Web.Auth;
using Fortino.Web.Chapters.Ch08.Scenarios;
using Fortino.Web.Data;
using Fortino.Web.Lab;
using Microsoft.EntityFrameworkCore;

namespace Fortino.Web.Chapters.Ch08;

// Capitolo 8 — Mass assignment e overposting.
// Gli indirizzi sono quelli veri dell'applicazione, registrati una volta per versione:
//   /account/profile                     → Vulnerable.ProfileApi   o Secure.ProfileApi
//   /portal/api/orders/{orderId}/lines   → Vulnerable.OrderLineApi o Secure.OrderLineApi
// Risponde la versione scelta dal cookie della prova; fuori da una prova, la corretta.
// Il lettore chiama questi indirizzi dalla console del browser.
public sealed class Ch08Module : ILabChapterModule
{
    public int Chapter => 8;

    public void AddServices(IServiceCollection services)
    {
        services.AddDbContext<Ch08DbContext>((sp, options) =>
            options.UseSqlite(sp.GetRequiredService<DbConnectionFactory>().ConnectionString));

        services.AddSingleton<ILabCase, RoleEscalationCase>();
        services.AddSingleton<ILabCase, ClientPriceCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // 8.1 Il profilo è di ogni utente collegato.
        Vulnerable.ProfileApi.Map(app.MapTrialVariant(Chapter, "role-escalation", CodeVariant.Vulnerable)
            .MapGroup("/account").RequireAuthorization());
        Secure.ProfileApi.Map(app.MapTrialVariant(Chapter, "role-escalation", CodeVariant.Secure)
            .MapGroup("/account").RequireAuthorization());

        // 8.2 Le righe d'ordine sono del portale clienti.
        Vulnerable.OrderLineApi.Map(app.MapTrialVariant(Chapter, "client-price", CodeVariant.Vulnerable)
            .MapGroup("/portal/api").RequireAuthorization(Policies.Customer));
        Secure.OrderLineApi.Map(app.MapTrialVariant(Chapter, "client-price", CodeVariant.Secure)
            .MapGroup("/portal/api").RequireAuthorization(Policies.Customer));
    }
}
