using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch13;

// Capitolo 13 — Gestione di errori ed eccezioni.
// Gli indirizzi sono quelli veri dell'applicazione, registrati una volta per versione; risponde
// quella scelta dal cookie della prova, fuori da una prova la corretta:
//   POST /invoices/issue         «Emetti la fattura», pagina di modifica di una bozza (caso 13.1)
//   GET  /orders/{id}/note       nota riservata dell'ordine, dalla barra degli indirizzi (caso 13.2)
// Il 13.1 mostra un errore che rivela troppo, il 13.2 un errore che apre la porta.
public sealed class Ch13Module : ILabChapterModule
{
    public int Chapter => 13;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.ErrorDetailsCase>();
        services.AddSingleton<ILabCase, Scenarios.FailOpenCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var logger = app.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Ch13.ErrorHandling");

        // 13.1 Errore che rivela troppo.
        Vulnerable.ErrorHandlingApi.MapIssue(
            app.MapTrialVariant(Chapter, "error-details", CodeVariant.Vulnerable).MapGroup("").RequireAuthorization());
        Secure.ErrorHandlingApi.MapIssue(
            app.MapTrialVariant(Chapter, "error-details", CodeVariant.Secure).MapGroup("").RequireAuthorization(), logger);

        // 13.2 Errore che apre la porta.
        Vulnerable.ErrorHandlingApi.MapNote(
            app.MapTrialVariant(Chapter, "fail-open", CodeVariant.Vulnerable).MapGroup("").RequireAuthorization());
        Secure.ErrorHandlingApi.MapNote(
            app.MapTrialVariant(Chapter, "fail-open", CodeVariant.Secure).MapGroup("").RequireAuthorization(), logger);
    }
}
