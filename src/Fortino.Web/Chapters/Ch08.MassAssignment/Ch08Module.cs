using Fortino.Web.Auth;
using Fortino.Web.Chapters.Ch08.Scenarios;
using Fortino.Web.Data;
using Fortino.Web.Lab;
using Microsoft.EntityFrameworkCore;

namespace Fortino.Web.Chapters.Ch08;

// Capitolo 8 — Mass assignment e overposting.
// Le due versioni hanno indirizzi diversi e codice diverso:
//   /lab/ch08/vulnerable/...  → Vulnerable.ProfileApi, Vulnerable.OrderLineApi
//   /lab/ch08/secure/...      → Secure.ProfileApi,     Secure.OrderLineApi
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
        // Il profilo è di ogni utente collegato; le righe d'ordine sono del portale clienti.
        var vulnerable = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Vulnerable));
        Vulnerable.ProfileApi.Map(vulnerable.MapGroup("").RequireAuthorization());
        Vulnerable.OrderLineApi.Map(vulnerable.MapGroup("").RequireAuthorization(Policies.Customer));

        var secure = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Secure));
        Secure.ProfileApi.Map(secure.MapGroup("").RequireAuthorization());
        Secure.OrderLineApi.Map(secure.MapGroup("").RequireAuthorization(Policies.Customer));
    }
}
