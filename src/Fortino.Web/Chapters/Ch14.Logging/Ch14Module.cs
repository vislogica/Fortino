using Fortino.Web.Auth;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch14;

// Capitolo 14 — Log e monitoraggio.
// Gli indirizzi sono quelli veri dell'applicazione, registrati una volta per versione; risponde
// quella scelta dal cookie della prova, fuori da una prova la corretta:
//   POST /login-attempt               tentativo di accesso        (caso 14.1, segreti nel registro)
//   POST /users/{id}/deactivate       disattivazione di un utente (caso 14.2, azione non tracciata)
// I logger hanno categoria "Fortino.Ch14...", così i messaggi compaiono nella pagina "Registro".
// I due casi si provano dalla console e l'effetto si legge nel Registro.
public sealed class Ch14Module : ILabChapterModule
{
    public int Chapter => 14;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.SecretsInLogCase>();
        services.AddSingleton<ILabCase, Scenarios.MissingAuditCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var loggerFactory = app.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var vulnLog = loggerFactory.CreateLogger("Fortino.Ch14.Vulnerable");
        var secLog = loggerFactory.CreateLogger("Fortino.Ch14.Secure");

        // 14.1 Segreti nel registro.
        Vulnerable.LoggingApi.MapLoginAttempt(
            app.MapTrialVariant(Chapter, "secrets-in-log", CodeVariant.Vulnerable).MapGroup("").RequireAuthorization(), vulnLog);
        Secure.LoggingApi.MapLoginAttempt(
            app.MapTrialVariant(Chapter, "secrets-in-log", CodeVariant.Secure).MapGroup("").RequireAuthorization(), secLog);

        // 14.2 Azione sensibile non tracciata.
        Vulnerable.LoggingApi.MapDeactivate(
            app.MapTrialVariant(Chapter, "missing-audit", CodeVariant.Vulnerable).MapGroup("").RequireAuthorization(Policies.Staff), vulnLog);
        Secure.LoggingApi.MapDeactivate(
            app.MapTrialVariant(Chapter, "missing-audit", CodeVariant.Secure).MapGroup("").RequireAuthorization(Policies.Staff), secLog);
    }
}
