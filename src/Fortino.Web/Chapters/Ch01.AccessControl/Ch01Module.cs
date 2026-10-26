using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch01;

// Capitolo 1 — Controllo degli accessi.
// I due casi si provano a mano dentro l'applicazione, nelle pagine vere:
//   caso 1.1  /portal/orders/{numero}  (Components/Pages/PortalOrder.razor)
//   caso 1.2  /customers               (Components/Pages/Customers.razor)
// Le pagine chiamano la versione vulnerabile o quella corretta secondo la prova in corso.
public sealed class Ch01Module : ILabChapterModule
{
    public int Chapter => 1;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.IdorCase>();
        services.AddSingleton<ILabCase, Scenarios.FunctionLevelCase>();

        services.AddSingleton<Vulnerable.OrderDetailReader>();
        services.AddSingleton<Secure.OrderDetailReader>();
        services.AddSingleton<Vulnerable.CustomerListReader>();
        services.AddSingleton<Secure.CustomerListReader>();
    }

    // Nessun indirizzo dedicato: il capitolo usa le pagine dell'applicazione.
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }
}
