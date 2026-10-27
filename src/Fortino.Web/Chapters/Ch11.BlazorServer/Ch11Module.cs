using Fortino.Web.Chapters.Ch11.Scenarios;
using Fortino.Web.Chapters.Ch11.Secure;
using Fortino.Web.Chapters.Ch11.Vulnerable;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch11;

// Capitolo 11 — Specificità di Blazor Server.
// Qui non ci sono API: i casi riguardano pagine Blazor e servizi. Le prove si fanno su due pagine
// vere del portale clienti, che scelgono la versione dal cookie della prova (fuori da una prova, la
// corretta):
//   /portal/invoices     "Le mie fatture"            (caso 11.1, servizio singleton condiviso)
//   /portal/quantities   "I miei ordini in bozza"    (caso 11.2, metodo [JSInvokable] dalla console)
public sealed class Ch11Module : ILabChapterModule
{
    public int Chapter => 11;

    public void AddServices(IServiceCollection services)
    {
        services.AddVulnerableMyInvoices();
        services.AddSecureMyInvoices();

        services.AddSingleton<ILabCase, SharedSingletonCase>();
        services.AddSingleton<ILabCase, JsInvokableCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app) { }
}
