using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch03.Scenarios;

// Base comune dei due casi del capitolo 3.
public abstract class Ch03Case : LabCaseBase
{
    public override int Chapter => 3;
    public override string Owasp => "OWASP A07:2025 Identification and Authentication Failures";
    public override string Iso27001 => "8.5, 5.17";
    public override string TestClass => "tests/Fortino.Tests/Ch03/Ch03AuthenticationTests.cs";

    // I casi del capitolo 3 si provano solo a mano (vedi Trial): non c'è la prova automatica.
    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Failed("Questo caso si prova a mano: segui i passi nella pagina del capitolo.");
        return Task.CompletedTask;
    }
}
