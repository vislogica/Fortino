using Fortino.Web.Lab;

namespace Fortino.Web.Infrastructure;

// Gli header che dicono al browser se una risposta di Fortino può essere mostrata dentro un altro
// sito, in una cornice (capitolo 4, caso 4.1). Si scrivono appena prima di mandare ogni risposta,
// dopo quelli che ASP.NET Core aggiunge da solo. La decisione la prende il codice del capitolo 4:
// fuori da una prova è sempre la versione corretta.
public static class FrameProtection
{
    public static IApplicationBuilder UseFrameProtection(this IApplicationBuilder app) =>
        app.Use(async (http, next) =>
        {
            http.Response.OnStarting(() =>
            {
                var trial = new LabTrialState(ActiveTrial.Parse(http.Request.Cookies[ActiveTrial.CookieName]));
                if (trial.VariantFor(4, "clickjacking") == CodeVariant.Vulnerable)
                    Chapters.Ch04.Vulnerable.FrameHeaders.Apply(http.Response);
                else
                    Chapters.Ch04.Secure.FrameHeaders.Apply(http.Response);
                return Task.CompletedTask;
            });
            await next();
        });
}
