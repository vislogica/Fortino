using Fortino.Web.Auth;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch10;

// Capitolo 10 — Consumo di risorse e blocco del servizio.
// Gli indirizzi sono quelli veri dell'applicazione, registrati una volta per versione; risponde
// quella scelta dal cookie della prova, fuori da una prova la corretta:
//   POST /documents/check-image   «Controlla un'immagine», pagina Documenti   (caso 10.1)
//   POST /notes/validate          «Controlla un testo», pagina Documenti      (caso 10.2)
//   GET  /orders/export?count=N   «Esporta in ZIP», pagina Ordini             (caso 10.3)
// I tre casi si provano dai moduli delle pagine; la pagina mostra l'effetto (memoria, tempo, dimensione).
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

        // Il file di prova del caso 10.1, da scaricare e poi caricare con «Controlla un'immagine».
        app.MapGet("/lab/file/immagine-enorme.png", () =>
                Results.File(Ch10Header.PngHeader(5000, 5000), "image/png", "immagine-enorme.png"))
            .RequireAuthorization(Policies.Staff);
    }

    private void Map(IEndpointRouteBuilder app, string caseSlug,
        Action<RouteGroupBuilder> vulnerable, Action<RouteGroupBuilder> secure)
    {
        vulnerable(app.MapTrialVariant(Chapter, caseSlug, CodeVariant.Vulnerable).MapGroup("").RequireAuthorization(Policies.Staff));
        secure(app.MapTrialVariant(Chapter, caseSlug, CodeVariant.Secure).MapGroup("").RequireAuthorization(Policies.Staff));
    }
}
