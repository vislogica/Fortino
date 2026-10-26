using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch02.Scenarios;

// Base comune dei due casi del capitolo 2.
public abstract class Ch02Case : LabCaseBase
{
    public override int Chapter => 2;
    public override string Owasp => "OWASP A03:2025 Injection";
    public override string Iso27001 => "8.26, 8.28";
    public override string TestClass => "tests/Fortino.Tests/Ch02/Ch02InjectionTests.cs";

    // I casi del capitolo 2 si provano solo a mano (vedi Trial): non c'è la prova automatica.
    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
