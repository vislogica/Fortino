using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch04;

// Capitolo 4 — Configurazione e header di sicurezza.
//   /lab/ch04/vulnerable/...  versione vulnerabile
//   /lab/ch04/secure/...      versione corretta
public sealed class Ch04Module : ILabChapterModule
{
    public int Chapter => 4;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.ClickjackingCase>();
        services.AddSingleton<ILabCase, Scenarios.OpenCorsCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var vulnerable = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Vulnerable)).RequireAuthorization();
        Vulnerable.HeadersApi.Map(vulnerable);

        var secure = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Secure)).RequireAuthorization();
        Secure.HeadersApi.Map(secure);
    }
}
