using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch13;

// Capitolo 13 — Gestione di errori ed eccezioni.
// Gli indirizzi sono quelli veri dell'applicazione, registrati una volta per versione; risponde
// quella scelta dal cookie della prova, fuori da una prova la corretta:
//   POST /invoices/issue         emissione di una fattura   (caso 13.1, errore che rivela troppo)
//   GET  /orders/{id}/note       nota riservata dell'ordine (caso 13.2, errore che apre la porta)
// I due casi si provano dalla console del browser.
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
