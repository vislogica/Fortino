using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch14;

// Capitolo 14 — Log e monitoraggio.
// Qui non ci sono indirizzi propri: i due casi si provano con le pagine vere dell'applicazione.
//   14.1 (segreti nel registro): la maschera di login reale registra il tentativo di accesso
//        (con la password nella versione vulnerabile) — vedi Auth/AuthEndpoints.
//   14.2 (azione non tracciata): il pulsante «Disattiva» della pagina Utenti lascia o no una voce
//        di audit — vedi Infrastructure/UserAdminEndpoints.
// In tutti e due il codice del capitolo (LoggingApi) viene richiamato durante la prova; i logger
// hanno categoria "Fortino.Ch14...", così i messaggi compaiono nella pagina "Registro".
public sealed class Ch14Module : ILabChapterModule
{
    public int Chapter => 14;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.SecretsInLogCase>();
        services.AddSingleton<ILabCase, Scenarios.MissingAuditCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app) { }
}
