using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch17.Scenarios;

// Base comune dei due casi del capitolo 17.
public abstract class Ch17Case : LabCaseBase
{
    public override int Chapter => 17;
    public override string Owasp => "OWASP LLM01 Prompt Injection / LLM06 Excessive Agency";
    public override string Iso27001 => "8.26, 8.27";
    public override string TestClass => "tests/Fortino.Tests/Ch17/Ch17AiRisksTests.cs";
}
