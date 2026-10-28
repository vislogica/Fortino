using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch04.Scenarios;

// Base comune dei due casi del capitolo 4.
public abstract class Ch04Case : LabCaseBase
{
    public override int Chapter => 4;
    public override string Owasp => "OWASP A02:2025 Security Misconfiguration";
    public override string Iso27001 => "8.9, 8.26";
    public override string TestClass => "tests/Fortino.Tests/Ch04/Ch04SecurityHeadersTests.cs";
}
