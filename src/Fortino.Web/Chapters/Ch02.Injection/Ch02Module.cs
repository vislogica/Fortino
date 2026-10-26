using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch02;

// Capitolo 2 — Iniezioni.
//   /lab/ch02/vulnerable/...  versione vulnerabile
//   /lab/ch02/secure/...      versione corretta
public sealed class Ch02Module : ILabChapterModule
{
    public int Chapter => 2;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.SqlInjectionCase>();
        services.AddSingleton<ILabCase, Scenarios.XssCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var vulnerable = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Vulnerable)).RequireAuthorization();
        Vulnerable.InjectionApi.Map(vulnerable);

        var secure = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Secure)).RequireAuthorization();
        Secure.InjectionApi.Map(secure);
    }
}
