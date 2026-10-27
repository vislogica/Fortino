using Fortino.Web.Auth;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch10;

// Capitolo 10 — Consumo di risorse e blocco del servizio.
// Gli indirizzi sono quelli veri dell'applicazione, registrati una volta per versione; risponde
// quella scelta dal cookie della prova, fuori da una prova la corretta:
//   POST /documents/check-image   controllo di un'immagine   (caso 10.1)
//   POST /notes/validate          validazione di un testo    (caso 10.2)
//   GET  /orders/export?count=N   esportazione in ZIP        (caso 10.3)
// I tre casi si provano dalla console del browser e si misura l'effetto (memoria, tempo, dimensione).
public sealed class Ch10Module : ILabChapterModule
{
    public int Chapter => 10;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.HugeImageCase>();
        services.AddSingleton<ILabCase, Scenarios.SlowRegexCase>();
        services.AddSingleton<ILabCase, Scenarios.UnboundedExportCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        Map(app, "huge-image", Vulnerable.ToolsApi.MapCheckImage, Secure.ToolsApi.MapCheckImage);
        Map(app, "slow-regex", Vulnerable.ToolsApi.MapValidate, Secure.ToolsApi.MapValidate);
        Map(app, "unbounded-export", Vulnerable.ToolsApi.MapExport, Secure.ToolsApi.MapExport);
    }

    private void Map(IEndpointRouteBuilder app, string caseSlug,
        Action<RouteGroupBuilder> vulnerable, Action<RouteGroupBuilder> secure)
    {
        vulnerable(app.MapTrialVariant(Chapter, caseSlug, CodeVariant.Vulnerable).MapGroup("").RequireAuthorization(Policies.Staff));
        secure(app.MapTrialVariant(Chapter, caseSlug, CodeVariant.Secure).MapGroup("").RequireAuthorization(Policies.Staff));
    }
}
