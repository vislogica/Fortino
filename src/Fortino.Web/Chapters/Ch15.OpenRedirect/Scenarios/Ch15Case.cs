using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch15.Scenarios;

// Base comune dei due casi del capitolo 15.
public abstract class Ch15Case : LabCaseBase
{
    public override int Chapter => 15;
    public override string Owasp => "OWASP A01:2025 Broken Access Control / A05 Security Misconfiguration";
    public override string Iso27001 => "8.26";
    public override string TestClass => "tests/Fortino.Tests/Ch15/Ch15OpenRedirectTests.cs";
}
