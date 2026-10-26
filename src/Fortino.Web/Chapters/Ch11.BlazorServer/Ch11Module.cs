using Fortino.Web.Chapters.Ch11.Scenarios;
using Fortino.Web.Chapters.Ch11.Secure;
using Fortino.Web.Chapters.Ch11.Vulnerable;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch11;

// Capitolo 11 — Specificità di Blazor Server.
// Qui non ci sono API: i casi riguardano pagine Blazor e servizi. Le pagine delle due versioni sono
//   /lab/ch11/vulnerable/my-invoices   e /lab/ch11/secure/my-invoices    (caso 11.1)
//   /lab/ch11/vulnerable/quantities    e /lab/ch11/secure/quantities     (caso 11.2)
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
