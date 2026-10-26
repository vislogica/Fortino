using System.Net.Http.Json;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch16.Scenarios;

// Caso 16.1: un buono sconto monouso viene applicato più volte con richieste simultanee.
public sealed class CouponReuseCase : Ch16Case
{
    public override string Slug => "coupon-reuse";
    public override IReadOnlyList<string> LabUsers => ["lumen"];
    public override string Title => "Buono sconto usato più volte";
    public override string Summary =>
        "Lo stesso buono monouso viene applicato con richieste simultanee: la versione vulnerabile lo accetta più volte. La corretta lo accetta una volta sola.";
    public override string BookReference => "capitolo 16, caso 16.1";
    public override CodeLocation VulnerableCode => new("Ch16.RaceCondition/Vulnerable/RaceApi.cs", "coupon-vulnerabile");
    public override CodeLocation SecureCode => new("Ch16.RaceCondition/Secure/RaceApi.cs", "coupon-corretto");

    private const string Code = "BENVENUTO10";
    private const int Attempts = 6;

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log($"Il buono monouso \"{Code}\" viene applicato {Attempts} volte nello stesso istante, su ordini diversi.");

        var tasks = Enumerable.Range(1, Attempts).Select(orderId =>
            client.PostAsJsonAsync($"{Api(variant)}/coupons/redeem", new { Code, OrderId = orderId }, ct));
        var responses = await Task.WhenAll(tasks);

        var accepted = responses.Count(r => r.IsSuccessStatusCode);
        foreach (var r in responses) r.Dispose();
        run.Log($"Richieste accettate: {accepted} su {Attempts}.");

        if (accepted > 1)
            run.Succeeded($"Il buono è stato applicato {accepted} volte invece di una. Lo stesso sconto " +
                          "monouso vale così su più ordini.");
        else
            run.Blocked("Il buono è stato applicato una volta sola: le altre richieste hanno ricevuto un conflitto.");
    }
}
