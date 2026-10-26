using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch06;

// Capitolo 6 — Segreti e chiavi di Data Protection.
//   /lab/ch06/vulnerable/...  versione vulnerabile (caso 6.1)
//   /lab/ch06/secure/...      versione corretta (caso 6.1)
// Il caso 6.2 non ha endpoint: lavora direttamente sulle chiavi di Data Protection.
public sealed class Ch06Module : ILabChapterModule
{
    public int Chapter => 6;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.SecretInCodeCase>();
        services.AddSingleton<ILabCase, Scenarios.DataProtectionKeysCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var vulnerable = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Vulnerable)).RequireAuthorization();
        Vulnerable.SecretApi.Map(vulnerable);

        var secure = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Secure)).RequireAuthorization();
        Secure.SecretApi.Map(secure);
    }
}
