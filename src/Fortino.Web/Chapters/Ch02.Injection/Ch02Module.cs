using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch02;

// Capitolo 2 — Iniezioni.
// I due casi si provano a mano dentro l'applicazione, nelle pagine vere:
//   caso 2.1  /portal?cerca=...        (Components/Pages/Portal.razor)
//   caso 2.2  /orders/{numero}         (Components/Pages/StaffOrder.razor; la nota si scrive
//                                       nel portale, in Components/Pages/PortalOrder.razor)
// Le pagine chiamano la versione vulnerabile o quella corretta secondo la prova in corso.
public sealed class Ch02Module : ILabChapterModule
{
    public int Chapter => 2;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.SqlInjectionCase>();
        services.AddSingleton<ILabCase, Scenarios.XssCase>();

        services.AddSingleton<Vulnerable.OrderSearchReader>();
        services.AddSingleton<Secure.OrderSearchReader>();
        services.AddSingleton<Vulnerable.OrderNoteRenderer>();
        services.AddSingleton<Secure.OrderNoteRenderer>();
    }

    // Nessun indirizzo dedicato: il capitolo usa le pagine dell'applicazione.
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
    }
}
