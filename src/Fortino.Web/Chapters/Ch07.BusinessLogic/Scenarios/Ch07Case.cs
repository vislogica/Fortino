using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch07.Scenarios;

// Base comune dei quattro casi del capitolo 7.
public abstract class Ch07Case : LabCaseBase
{
    public override int Chapter => 7;
    public override string Owasp => "OWASP A06:2025 Insecure Design";
    public override string Iso27001 => "8.26, 8.27, 8.29";
    public override string TestClass => "tests/Fortino.Tests/Ch07/Ch07BusinessLogicTests.cs";
}
