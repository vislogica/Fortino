using Fortino.Web.Auth;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch17;

// Capitolo 17 — Rischi delle app che usano l'AI.
// La pagina «Assistente» del portale clienti (/portal/assistente) manda ogni domanda a uno dei due
// indirizzi dell'assistente, registrati una volta per versione:
//   POST /assistant/ask      risposta libera, con le note degli ordini   (caso 17.1, prompt injection)
//   POST /assistant/orders   gli ordini di un cliente                     (caso 17.2, accesso eccessivo)
// Risponde la versione scelta dal cookie della prova; fuori da una prova, la corretta.
public sealed class Ch17Module : ILabChapterModule
{
    public int Chapter => 17;

    public void AddServices(IServiceCollection services)
    {
        services.AddSingleton<ILabCase, Scenarios.PromptInjectionCase>();
        services.AddSingleton<ILabCase, Scenarios.ExcessiveAccessCase>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        Vulnerable.AssistantApi.MapAsk(app.MapTrialVariant(Chapter, "prompt-injection", CodeVariant.Vulnerable)
            .MapGroup("").RequireAuthorization(Policies.Customer));
        Secure.AssistantApi.MapAsk(app.MapTrialVariant(Chapter, "prompt-injection", CodeVariant.Secure)
            .MapGroup("").RequireAuthorization(Policies.Customer));

        Vulnerable.AssistantApi.MapOrders(app.MapTrialVariant(Chapter, "excessive-access", CodeVariant.Vulnerable)
            .MapGroup("").RequireAuthorization(Policies.Customer));
        Secure.AssistantApi.MapOrders(app.MapTrialVariant(Chapter, "excessive-access", CodeVariant.Secure)
            .MapGroup("").RequireAuthorization(Policies.Customer));
    }
}
