using Fortino.Web.Auth;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch12;

// Capitolo 12 — Crittografia debole.
// Gli indirizzi sono quelli veri dell'applicazione, registrati una volta per versione; risponde
// quella scelta dal cookie della prova, fuori da una prova la corretta:
//   POST /password/hash          hash di una password        (caso 12.1, hash debole)
//   POST /password/reset-token   codice di reset             (caso 12.2, codice prevedibile)
// I due casi si provano dalla console: si guarda se due risultati sono uguali o diversi.
public sealed class Ch12Module : ILabChapterModule
{
    public int Chapter => 12;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.WeakHashCase>();
        services.AddSingleton<ILabCase, Scenarios.PredictableTokenCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // 12.1 Password con hash debole.
        Vulnerable.PasswordApi.MapHash(
            app.MapTrialVariant(Chapter, "weak-hash", CodeVariant.Vulnerable).MapGroup("").RequireAuthorization());
        Secure.PasswordApi.MapHash(
            app.MapTrialVariant(Chapter, "weak-hash", CodeVariant.Secure).MapGroup("").RequireAuthorization());

        // 12.2 Codice di reset prevedibile.
        Vulnerable.PasswordApi.MapResetToken(
            app.MapTrialVariant(Chapter, "predictable-token", CodeVariant.Vulnerable).MapGroup("").RequireAuthorization());
        Secure.PasswordApi.MapResetToken(
            app.MapTrialVariant(Chapter, "predictable-token", CodeVariant.Secure).MapGroup("").RequireAuthorization());
    }
}
