using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch04;

// Capitolo 4 — Configurazione e header di sicurezza.
// I due casi si provano a mano dentro l'applicazione, nelle pagine vere:
//   caso 4.1  la pagina «Utenti» /users, incorniciata dal sito del truffatore (porta 5002).
//             Gli header sulle cornici si scrivono in Infrastructure/FrameProtection.cs.
//   caso 4.2  il riepilogo del portale /portal/api/situazione, letto dal sito del truffatore.
//             Il permesso CORS si scrive in Infrastructure/PortalEndpoints.cs.
// Il codice chiama la versione vulnerabile o quella corretta secondo la prova in corso.
public sealed class Ch04Module : ILabChapterModule
{
    public int Chapter => 4;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.ClickjackingCase>();
        services.AddSingleton<ILabCase, Scenarios.OpenCorsCase>();
    }

    // Niente indirizzi dedicati nell'applicazione. Si monta solo il "sito del truffatore", servito
    // sulla porta 5002 (vedi Attacker/AttackerSite.cs), usato dalle prove a mano dei due casi.
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        Attacker.AttackerSite.Map(app);
    }
}
