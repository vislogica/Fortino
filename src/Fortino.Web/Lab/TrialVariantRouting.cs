using Microsoft.AspNetCore.Routing.Matching;

namespace Fortino.Web.Lab;

// Un indirizzo vero dell'applicazione registrato due volte, una per versione del codice.
// I capitoli che scrivono gli endpoint direttamente (Map...) registrano lo stesso indirizzo
// con la versione vulnerabile e con quella corretta; a ogni richiesta risponde quella scelta
// dal cookie della prova. Fuori da una prova risponde sempre la corretta.
public sealed record TrialVariantMetadata(int Chapter, string CaseSlug, CodeVariant Variant);

public static class TrialVariantRouting
{
    // Il gruppo degli endpoint di una versione: stesso indirizzo dell'altra, risponde solo quando tocca a lui.
    public static RouteGroupBuilder MapTrialVariant(this IEndpointRouteBuilder app, int chapter, string caseSlug, CodeVariant variant)
    {
        var group = app.MapGroup("");
        group.WithMetadata(new TrialVariantMetadata(chapter, caseSlug, variant));
        return group;
    }
}

// Durante la scelta dell'endpoint scarta la versione che non è in prova.
public sealed class TrialVariantMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
{
    public override int Order => 0;

    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints) =>
        endpoints.Any(e => e.Metadata.GetMetadata<TrialVariantMetadata>() is not null);

    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        var trial = new LabTrialState(ActiveTrial.Parse(httpContext.Request.Cookies[ActiveTrial.CookieName]));
        for (int i = 0; i < candidates.Count; i++)
        {
            var metadata = candidates[i].Endpoint.Metadata.GetMetadata<TrialVariantMetadata>();
            if (metadata is null) continue;
            if (trial.VariantFor(metadata.Chapter, metadata.CaseSlug) != metadata.Variant)
                candidates.SetValidity(i, false);
        }
        return Task.CompletedTask;
    }
}
