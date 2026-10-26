using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch15;

// Capitolo 15 — Open redirect e Host header.
//   /lab/ch15/vulnerable/...  versione vulnerabile
//   /lab/ch15/secure/...      versione corretta
public sealed class Ch15Module : ILabChapterModule
{
    public int Chapter => 15;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.OpenRedirectCase>();
        services.AddSingleton<ILabCase, Scenarios.HostHeaderCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var vulnerable = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Vulnerable)).RequireAuthorization();
        Vulnerable.RedirectApi.Map(vulnerable, LabContext.BasePath(Chapter, CodeVariant.Vulnerable) + "/login");

        var secure = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Secure)).RequireAuthorization();
        Secure.RedirectApi.Map(secure, LabContext.BasePath(Chapter, CodeVariant.Secure) + "/login");
    }
}
