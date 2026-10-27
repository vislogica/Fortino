using System.Net.Http.Json;
using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Lab;

// Base comune dei casi: gli strumenti che servono a quasi tutti gli attacchi.
public abstract class LabCaseBase : ILabCase
{
    public abstract int Chapter { get; }
    public abstract string Slug { get; }
    public abstract string Title { get; }
    public abstract string Summary { get; }
    public abstract string BookReference { get; }
    public abstract string Owasp { get; }
    public abstract string Iso27001 { get; }
    public abstract CodeLocation VulnerableCode { get; }
    public abstract CodeLocation SecureCode { get; }
    public abstract string TestClass { get; }

    public virtual bool VulnerableAvailable => true;

    public virtual bool IsProcess => false;

    public virtual string? ProcessCommand => null;

    public virtual string? ManualPage => null;

    public virtual string? ManualHint => null;

    public virtual ManualTrial? Trial => null;

    public virtual IReadOnlyList<string> LabUsers => [];

    public abstract Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct);

    // Indirizzo base della versione scelta: /lab/chNN/vulnerable oppure /lab/chNN/secure.
    protected string Api(CodeVariant variant) => LabContext.BasePath(Chapter, variant);

    // Legge un valore direttamente dal database, per verificare cosa è successo davvero.
    protected static async Task<T> ReadAsync<T>(LabContext context, string sql, object? args = null)
    {
        await using var db = await context.Services.GetRequiredService<DbConnectionFactory>().OpenAsync();
        return await db.QuerySingleAsync<T>(sql, args);
    }

    // Scrive nel diario la risposta del server, con il messaggio d'errore se c'è.
    protected static async Task LogResponseAsync(AttackRun run, HttpResponseMessage response, CancellationToken ct)
    {
        var line = $"Risposta del server: {(int)response.StatusCode} {response.ReasonPhrase}";
        if (!response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType == "application/problem+json")
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemBody>(ct);
            if (!string.IsNullOrEmpty(problem?.Detail)) line += $", \"{problem.Detail}\"";
        }
        run.Log(line + ".");
    }

    private sealed record ProblemBody(string? Detail);
}
