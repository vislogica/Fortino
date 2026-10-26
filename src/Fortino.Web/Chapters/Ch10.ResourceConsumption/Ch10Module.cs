using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch10;

// Capitolo 10 — Consumo di risorse e blocco del servizio.
// In questa edizione è presente solo la versione corretta dei tre casi (funzioni di difesa con i
// loro test). La versione vulnerabile e gli attacchi verranno integrati più avanti.
public sealed class Ch10Module : ILabChapterModule
{
    public int Chapter => 10;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.HugeImageCase>();
        services.AddSingleton<ILabCase, Scenarios.SlowRegexCase>();
        services.AddSingleton<ILabCase, Scenarios.UnboundedExportCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app) { }
}
