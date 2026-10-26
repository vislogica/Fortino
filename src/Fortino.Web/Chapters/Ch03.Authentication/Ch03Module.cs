using Fortino.Web.Auth;
using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch03;

// Capitolo 3 — Autenticazione e sessioni.
// I due casi si provano a mano dentro l'applicazione, nelle pagine vere:
//   caso 3.1  la pagina di accesso /login  (il modulo arriva a /auth/login, Auth/AuthEndpoints.cs)
//   caso 3.2  ogni pagina, dopo che l'amministratore ha disattivato l'utente in /users
//             (il cookie si ricontrolla in Auth/SessionCheck.cs)
// Il codice chiama la versione vulnerabile o quella corretta secondo la prova in corso.
public sealed class Ch03Module : ILabChapterModule
{
    public int Chapter => 3;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.BruteForceCase>();
        services.AddSingleton<ILabCase, Scenarios.StaleSessionCase>();

        services.AddSingleton<LoginThrottle>();
        services.AddSingleton<ILabStateReset, LoginThrottleReset>();
        services.AddSingleton<ILabStateReset, DemoUsersReactivation>();
        services.AddSingleton<Vulnerable.LoginChecker>();
        services.AddSingleton<Secure.LoginChecker>();
        services.AddSingleton<Vulnerable.SessionValidator>();
        services.AddSingleton<Secure.SessionValidator>();
    }

    // Nessun indirizzo dedicato: il capitolo usa le pagine dell'applicazione.
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }

    // A ogni prova si cancellano gli errori degli utenti dimostrativi: un nome bloccato nella
    // prova del caso 3.1 non deve restare bloccato nella prova successiva.
    private sealed class LoginThrottleReset(LoginThrottle throttle) : ILabStateReset
    {
        public Task ResetAsync()
        {
            foreach (var user in SeedData.Users)
                throttle.Reset(user.Username);
            return Task.CompletedTask;
        }
    }

    // A ogni prova si riattivano gli utenti dimostrativi: dopo la versione vulnerabile del caso 3.2
    // Ottica Lumen è disattivata, e senza questo non potrebbe entrare nella prova successiva.
    private sealed class DemoUsersReactivation(UserStore users) : ILabStateReset
    {
        public async Task ResetAsync()
        {
            foreach (var user in await users.GetAllAsync())
                if (!user.IsActive && SeedData.Users.Any(u => u.Username == user.Username))
                    await users.SetActiveAsync(user.Id, true);
        }
    }
}
