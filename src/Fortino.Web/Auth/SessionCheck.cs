using Fortino.Web.Lab;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Fortino.Web.Auth;

// A ogni richiesta con il cookie di sessione, ASP.NET Core chiede qui se il cookie vale ancora
// (evento OnValidatePrincipal, registrato in Program.cs). La decisione la prende il codice del
// capitolo 3 (caso 3.2): fuori da una prova è sempre la versione corretta.
public static class SessionCheck
{
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        if (context.Principal is null) return;

        var trial = new LabTrialState(ActiveTrial.Parse(context.Request.Cookies[ActiveTrial.CookieName]));
        var services = context.HttpContext.RequestServices;
        bool isValid = trial.VariantFor(3, "stale-session") == CodeVariant.Vulnerable
            ? await services.GetRequiredService<Chapters.Ch03.Vulnerable.SessionValidator>().IsStillValidAsync(context.Principal)
            : await services.GetRequiredService<Chapters.Ch03.Secure.SessionValidator>().IsStillValidAsync(context.Principal);

        // Sessione non più valida: la richiesta continua come se l'utente non fosse collegato,
        // e il cookie viene cancellato.
        if (!isValid)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
