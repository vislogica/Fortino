using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch18;

// Capitolo 18 — Deserializzazione insicura e XXE.
//   /lab/ch18/vulnerable/...  versione vulnerabile
//   /lab/ch18/secure/...      versione corretta
public sealed class Ch18Module : ILabChapterModule
{
    public int Chapter => 18;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.XxeCase>();
        services.AddSingleton<ILabCase, Scenarios.UnsafeDeserializationCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var vulnerable = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Vulnerable)).RequireAuthorization();
        Vulnerable.ImportApi.Map(vulnerable);

        var secure = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Secure)).RequireAuthorization();
        Secure.ImportApi.Map(secure);
    }
}
