using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch15;

// Capitolo 15 — Open redirect e Host header.
//   caso 15.1  il login vero di Fortino (/auth/login): dopo l'accesso, dove si torna lo decide
//              il codice del capitolo, scelto dal cookie della prova (vedi AuthEndpoints);
//   caso 15.2  «Password dimenticata?» (POST /forgot-password), registrato una volta per versione.
// Le pagine del truffatore (email trappola, pannello, pagine finte) stanno in Attacker/.
public sealed class Ch15Module : ILabChapterModule
{
    public int Chapter => 15;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.OpenRedirectCase>();
        services.AddSingleton<ILabCase, Scenarios.HostHeaderCase>();
        services.AddHttpClient(Attacker.AttackerPages.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // Il certificato di sviluppo di localhost: il truffatore chiama Fortino senza verificarlo.
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                UseCookies = false,
                AllowAutoRedirect = false,
            });
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        Vulnerable.RedirectApi.MapForgotPassword(
            app.MapTrialVariant(Chapter, "host-header", CodeVariant.Vulnerable).MapGroup("").AllowAnonymous());
        Secure.RedirectApi.MapForgotPassword(
            app.MapTrialVariant(Chapter, "host-header", CodeVariant.Secure).MapGroup("").AllowAnonymous());

        Attacker.AttackerPages.Map(app);
    }
}
