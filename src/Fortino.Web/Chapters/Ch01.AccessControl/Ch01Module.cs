using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch01;

// Capitolo 1 — Controllo degli accessi.
//   /lab/ch01/vulnerable/...  versione vulnerabile
//   /lab/ch01/secure/...      versione corretta
public sealed class Ch01Module : ILabChapterModule
{
    public int Chapter => 1;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.IdorCase>();
        services.AddSingleton<ILabCase, Scenarios.FunctionLevelCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var vulnerable = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Vulnerable)).RequireAuthorization();
        Vulnerable.AccessApi.Map(vulnerable);

        var secure = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Secure)).RequireAuthorization();
        Secure.AccessApi.Map(secure);
    }
}
