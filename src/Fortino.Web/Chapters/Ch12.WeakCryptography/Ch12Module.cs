using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch12;

// Capitolo 12 — Crittografia debole.
// Le azioni sono quelle vere della pagina /users, riservata agli amministratori, registrate una volta
// per versione; risponde quella scelta dal cookie della prova, fuori da una prova la corretta:
//   POST /users/nuovo            «Aggiungi un utente»          (caso 12.1, hash debole)
//   POST /users/reset-password   «Invia il codice di reset»    (caso 12.2, codice prevedibile)
// Il risultato si guarda nella pagina «Vista database» (le impronte delle password) e nella
// «Posta finta» (i codici arrivati agli utenti).
public sealed class Ch12Module : ILabChapterModule
{
    public int Chapter => 12;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<Ch12Users>();
        services.AddSingleton<ILabCase, Scenarios.WeakHashCase>();
        services.AddSingleton<ILabCase, Scenarios.PredictableTokenCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // 12.1 Password con hash debole.
        Vulnerable.PasswordApi.MapCreateUser(
            app.MapTrialVariant(Chapter, "weak-hash", CodeVariant.Vulnerable).MapGroup("").RequireAuthorization(p => p.RequireRole("Admin")));
        Secure.PasswordApi.MapCreateUser(
            app.MapTrialVariant(Chapter, "weak-hash", CodeVariant.Secure).MapGroup("").RequireAuthorization(p => p.RequireRole("Admin")));

        // 12.2 Codice di reset prevedibile.
        Vulnerable.PasswordApi.MapSendResetCodes(
            app.MapTrialVariant(Chapter, "predictable-token", CodeVariant.Vulnerable).MapGroup("").RequireAuthorization(p => p.RequireRole("Admin")));
        Secure.PasswordApi.MapSendResetCodes(
            app.MapTrialVariant(Chapter, "predictable-token", CodeVariant.Secure).MapGroup("").RequireAuthorization(p => p.RequireRole("Admin")));
    }
}
