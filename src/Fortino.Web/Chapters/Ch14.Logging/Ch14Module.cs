using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch14;

// Capitolo 14 — Log e monitoraggio.
//   /lab/ch14/vulnerable/...  versione vulnerabile
//   /lab/ch14/secure/...      versione corretta
// I logger hanno categoria "Fortino...", così i messaggi compaiono nella pagina "Registro".
public sealed class Ch14Module : ILabChapterModule
{
    public int Chapter => 14;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.SecretsInLogCase>();
        services.AddSingleton<ILabCase, Scenarios.MissingAuditCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var loggerFactory = app.ServiceProvider.GetRequiredService<ILoggerFactory>();

        var vulnerable = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Vulnerable)).RequireAuthorization();
        Vulnerable.LoggingApi.Map(vulnerable, loggerFactory.CreateLogger("Fortino.Ch14.Vulnerable"));

        var secure = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Secure)).RequireAuthorization();
        Secure.LoggingApi.Map(secure, loggerFactory.CreateLogger("Fortino.Ch14.Secure"));
    }
}
