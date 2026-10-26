using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch05;

// Capitolo 5 — Componenti vulnerabili e supply chain.
// Capitolo di processo: un solo caso, con la sola "versione corretta" (le abitudini di controllo).
public sealed class Ch05Module : ILabChapterModule
{
    public int Chapter => 5;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.VulnerableComponentsCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app) { }
}
