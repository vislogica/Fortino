using Fortino.Web.Infrastructure;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch14.Scenarios;

// Base comune dei due casi del capitolo 14.
public abstract class Ch14Case : LabCaseBase
{
    public override int Chapter => 14;
    public override string Owasp => "OWASP A09:2025 Security Logging and Alerting Failures";
    public override string Iso27001 => "8.15, 8.16";
    public override string TestClass => "tests/Fortino.Tests/Ch14/Ch14LoggingTests.cs";

    // Legge le righe del registro in memoria, per vedere cosa è stato (o non è stato) scritto.
    protected static IReadOnlyList<LogRecord> ReadLog(LabContext context) =>
        context.Services.GetRequiredService<InMemoryLogStore>().Snapshot();
}
