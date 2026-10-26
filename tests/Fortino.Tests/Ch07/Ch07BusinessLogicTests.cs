using Fortino.Web.Chapters.Ch07.Secure;
using Fortino.Web.Domain;
using Fortino.Web.Lab;
using Microsoft.Extensions.DependencyInjection;

namespace Fortino.Tests.Ch07;

// Capitolo 7 — Logica di business e progettazione insicura.
// Per ogni caso: sulla versione vulnerabile l'attacco riesce, su quella corretta viene bloccato.
public class Ch07BusinessLogicTests(FortinoFactory factory) : IClassFixture<FortinoFactory>
{
    #region libro: test-casi-abuso
    public static TheoryData<string> Cases => ["issued-invoice", "frozen-data", "skipped-state", "negative-quantity"];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Vulnerable_version_lets_the_attack_through(string slug)
    {
        // PASSO 1: si esegue l'attacco sulla versione vulnerabile.
        AttackRun run = await RunAsync(slug, CodeVariant.Vulnerable);

        // PASSO 2: l'attacco deve riuscire. Se non riesce, il messaggio mostra tutti i passi della prova.
        bool attackSucceeded = run.Outcome == AttackOutcome.Succeeded;
        Assert.True(attackSucceeded, Describe(run));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Secure_version_blocks_the_attack(string slug)
    {
        // PASSO 1: si esegue lo stesso attacco sulla versione corretta.
        AttackRun run = await RunAsync(slug, CodeVariant.Secure);

        // PASSO 2: l'attacco deve essere bloccato.
        bool attackBlocked = run.Outcome == AttackOutcome.Blocked;
        Assert.True(attackBlocked, Describe(run));
    }
    #endregion

    [Theory]
    [InlineData(OrderStatus.Draft, OrderStatus.Approved, true)]
    [InlineData(OrderStatus.Approved, OrderStatus.Shipped, true)]
    [InlineData(OrderStatus.Draft, OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Draft, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Approved, false)]
    public void Order_workflow_allows_only_the_planned_transitions(OrderStatus from, OrderStatus to, bool allowed) =>
        Assert.Equal(allowed, OrderWorkflow.CanMove(from, to));

    private async Task<AttackRun> RunAsync(string slug, CodeVariant variant)
    {
        _ = factory.Server;
        using var scope = factory.Services.CreateScope();
        var labCase = scope.ServiceProvider.GetRequiredService<LabRegistry>().Find(7, slug)!;
        return await scope.ServiceProvider.GetRequiredService<LabCaseRunner>().RunAsync(labCase, variant);
    }

    // In caso di fallimento il messaggio mostra il diario completo della prova.
    private static string Describe(AttackRun run) =>
        $"Esito {run.Outcome}: {run.Verdict}{Environment.NewLine}" + string.Join(Environment.NewLine, run.Steps.Select(s => s.Text));
}
