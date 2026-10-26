using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch03;

// Capitolo 3 — Autenticazione e sessioni.
//   /lab/ch03/vulnerable/...  versione vulnerabile
//   /lab/ch03/secure/...      versione corretta
public sealed class Ch03Module : ILabChapterModule
{
    public int Chapter => 3;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<LoginThrottle>();
        services.AddSingleton<ILabCase, Scenarios.BruteForceCase>();
        services.AddSingleton<ILabCase, Scenarios.StaleSessionCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var vulnerable = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Vulnerable));
        Vulnerable.AuthApi.Map(vulnerable);

        var secure = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Secure));
        Secure.AuthApi.Map(secure);
    }
}
