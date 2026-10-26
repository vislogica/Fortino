using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch13;

// Capitolo 13 — Gestione di errori ed eccezioni.
//   /lab/ch13/vulnerable/...  versione vulnerabile
//   /lab/ch13/secure/...      versione corretta
public sealed class Ch13Module : ILabChapterModule
{
    public int Chapter => 13;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.ErrorDetailsCase>();
        services.AddSingleton<ILabCase, Scenarios.FailOpenCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var loggerFactory = app.ServiceProvider.GetRequiredService<ILoggerFactory>();

        var vulnerable = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Vulnerable)).RequireAuthorization();
        Vulnerable.ErrorHandlingApi.Map(vulnerable);

        var secure = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Secure)).RequireAuthorization();
        Secure.ErrorHandlingApi.Map(secure, loggerFactory);
    }
}
