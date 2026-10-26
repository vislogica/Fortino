using Fortino.Web.Auth;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch09;

// Capitolo 9 — Upload, download e path traversal.
// Nota: in questa edizione del progetto è presente solo la versione corretta del caricamento e
// dello scaricamento dei documenti. La versione vulnerabile e i casi dimostrativi del Laboratorio
// non sono inclusi. Il testo del capitolo spiega l'attacco a parole.
//   /lab/ch09/secure/orders/{id}/documents   caricamento sicuro
//   /lab/ch09/secure/documents/{id}           scaricamento sicuro
public sealed class Ch09Module : ILabChapterModule
{
    public int Chapter => 9;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<DocumentStore>();

        // I due casi hanno lo stesso aspetto degli altri capitoli, ma per ora solo la versione
        // corretta è disponibile: la versione vulnerabile e l'attacco verranno integrati più avanti.
        services.AddSingleton<ILabCase, Scenarios.UploadPathTraversalCase>();
        services.AddSingleton<ILabCase, Scenarios.DownloadPathTraversalCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var secure = app.MapGroup(LabContext.BasePath(Chapter, CodeVariant.Secure)).RequireAuthorization(Policies.Staff);
        Secure.DocumentApi.Map(secure);
    }
}
