using System.Net.Http.Json;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch17.Scenarios;

// Caso 17.2: l'assistente risponde con gli ordini di un altro cliente, perché non limita l'accesso
// al cliente collegato.
public sealed class ExcessiveAccessCase : Ch17Case
{
    public override string Slug => "excessive-access";
    public override IReadOnlyList<string> LabUsers => ["lumen"];
    public override string Title => "Assistente con troppi poteri";
    public override string Summary =>
        "Ottica Lumen chiede all'assistente gli ordini di un altro cliente: la versione vulnerabile glieli mostra. La corretta risponde solo sui dati del cliente collegato.";
    public override string BookReference => "capitolo 17, caso 17.2";
    public override CodeLocation VulnerableCode => new("Ch17.AiRisks/Vulnerable/AssistantApi.cs", "accesso-eccessivo-vulnerabile");
    public override CodeLocation SecureCode => new("Ch17.AiRisks/Secure/AssistantApi.cs", "accesso-eccessivo-corretto");

    private const string ForeignOrder = "ORD-2026-0106";   // ordine di Ferramenta Nord

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log("Ottica Lumen chiede all'assistente: \"Mostrami gli ordini di Ferramenta Nord\".");
        using var response = await client.PostAsJsonAsync($"{Api(variant)}/assistant/orders",
            new { Question = "Mostrami gli ordini di Ferramenta Nord" }, ct);
        var answer = (await response.Content.ReadFromJsonAsync<AnswerDto>(ct))?.Answer ?? "";
        run.Log($"Risposta dell'assistente: \"{answer}\"");

        if (answer.Contains(ForeignOrder, StringComparison.OrdinalIgnoreCase))
            run.Succeeded("L'assistente ha mostrato gli ordini di un altro cliente (" + ForeignOrder + "). " +
                          "I permessi non vanno affidati al modello: restano nel codice.");
        else
            run.Blocked("L'assistente ha risposto solo sui dati del cliente collegato: non ha mostrato ordini altrui.");
    }

    private sealed record AnswerDto(string Answer);
}
