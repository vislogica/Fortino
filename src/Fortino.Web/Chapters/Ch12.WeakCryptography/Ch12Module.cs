using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch12;

// Capitolo 12 — Crittografia debole.
// In questa edizione è presente solo la versione corretta dei due casi (funzioni con i loro test).
// La versione vulnerabile e gli attacchi verranno integrati più avanti.
public sealed class Ch12Module : ILabChapterModule
{
    public int Chapter => 12;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.WeakHashCase>();
        services.AddSingleton<ILabCase, Scenarios.PredictableTokenCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app) { }
}
