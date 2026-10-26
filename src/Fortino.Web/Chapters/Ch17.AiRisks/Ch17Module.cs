using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch17;

// Capitolo 17 — Rischi delle app che usano l'AI.
//   /lab/ch17/vulnerable/...  versione vulnerabile
//   /lab/ch17/secure/...      versione corretta
public sealed class Ch17Module : ILabChapterModule
{
    public int Chapter => 17;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.PromptInjectionCase>();
        services.AddSingleton<ILabCase, Scenarios.ExcessiveAccessCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var vulnerable = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Vulnerable)).RequireAuthorization();
        Vulnerable.AssistantApi.Map(vulnerable);

        var secure = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Secure)).RequireAuthorization();
        Secure.AssistantApi.Map(secure);
    }
}
