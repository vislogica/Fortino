using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Fortino.Web.Domain;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch08.Scenarios;

// Caso 8.1: un cliente del portale aggiunge "role" alla modifica del suo profilo.
public sealed class RoleEscalationCase : Ch08Case
{
    public override string Slug => "role-escalation";
    public override IReadOnlyList<string> LabUsers => ["lumen"];
    public override string Title => "Cliente che diventa amministratore";
    public override string Summary =>
        "Nella modifica del proprio profilo, un cliente aggiunge il campo \"role\" alla richiesta e diventa amministratore.";
    public override string BookReference => "capitolo 8, caso 8.1";
    public override CodeLocation VulnerableCode => new("Ch08.MassAssignment/Vulnerable/ProfileApi.cs", "profilo-vulnerabile");
    public override CodeLocation SecureCode => new("Ch08.MassAssignment/Secure/ProfileApi.cs", "profilo-corretto");

    private const string UserSql =
        "SELECT DisplayName, Email, Role, CustomerId, IsActive FROM AppUser WHERE Username = 'lumen';";

    // La riga dell'utente così com'è nel database (l'hash della password non serve qui).
    private sealed record UserRow(string DisplayName, string Email, long Role, long? CustomerId, long IsActive)
    {
        public IEnumerable<(string Name, string Value)> Fields()
        {
            yield return ("DisplayName", DisplayName);
            yield return ("Email", Email);
            yield return ("Role", $"{(UserRole)Role} ({Role})");
            yield return ("CustomerId", CustomerId?.ToString() ?? "null");
            yield return ("IsActive", IsActive == 1 ? "true" : "false");
        }

        public override string ToString() => string.Join(", ", Fields().Select(f => $"{f.Name} = {f.Value}"));
    }

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);
        var before = await ReadAsync<UserRow>(context, UserSql);
        run.Log($"Utente lumen nel database PRIMA dell'attacco: {before}.");

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log("Entra Ottica Lumen, cliente del portale.");

        // 1. Si legge il profilo, come fa la pagina.
        var profile = await client.GetFromJsonAsync<JsonObject>($"{Api(variant)}/profile", ct)
            ?? throw new InvalidOperationException("Profilo vuoto.");
        run.Log($"GET /profile: il server restituisce i campi {string.Join(", ", profile.Select(p => p.Key))}.");

        // 2. Si rimanda il profilo con un nome nuovo, come fa la pagina, più un campo in più.
        profile["displayName"] = "Ottica Lumen S.n.c.";
        profile["role"] = (int)UserRole.Admin;
        run.Log($"PUT /profile con il nome nuovo e, in più, \"role\": 0 (Amministratore). Campi inviati: {Describe(profile)}.");
        using var response = await client.PutAsJsonAsync($"{Api(variant)}/profile", profile, ct);
        await LogResponseAsync(run, response, ct);

        // 3. Si rilegge l'utente dal database e si confrontano i campi.
        var after = await ReadAsync<UserRow>(context, UserSql);
        run.Log($"Utente lumen nel database DOPO l'attacco: {after}.");
        var changed = before.Fields().Zip(after.Fields())
            .Where(p => p.First.Value != p.Second.Value)
            .Select(p => $"{p.First.Name} da \"{p.First.Value}\" a \"{p.Second.Value}\"")
            .ToList();
        run.Log(changed.Count == 0 ? "Campi cambiati: nessuno." : $"Campi cambiati: {string.Join("; ", changed)}.");

        if ((UserRole)after.Role == UserRole.Admin)
            run.Succeeded("Ottica Lumen ora è Amministratore: al prossimo accesso vede e modifica tutto il gestionale. " +
                          "Il database resta così: premi \"Ripristina i dati\" per riportarla a cliente.");
        else
            run.Blocked("Il campo \"role\" non è passato: Ottica Lumen è ancora un cliente del portale.");
    }

    // I campi del JSON inviato, uno per uno; l'hash della password è accorciato per non riempire il diario.
    private static string Describe(JsonObject body) =>
        string.Join(", ", body.Select(p =>
        {
            var value = p.Value?.ToString() ?? "null";
            return $"{p.Key} = {(p.Key == "passwordHash" && value.Length > 12 ? value[..12] + "…" : value)}";
        }));
}
