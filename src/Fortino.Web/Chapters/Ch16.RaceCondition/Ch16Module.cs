using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch16;

// Capitolo 16 — Race condition.
//   /lab/ch16/vulnerable/...  versione vulnerabile
//   /lab/ch16/secure/...      versione corretta
public sealed class Ch16Module : ILabChapterModule
{
    public int Chapter => 16;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.CouponReuseCase>();
        services.AddSingleton<ILabCase, Scenarios.DoubleInvoiceCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var vulnerable = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Vulnerable)).RequireAuthorization();
        Vulnerable.RaceApi.Map(vulnerable);

        var secure = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Secure)).RequireAuthorization();
        Secure.RaceApi.Map(secure);
    }
}
