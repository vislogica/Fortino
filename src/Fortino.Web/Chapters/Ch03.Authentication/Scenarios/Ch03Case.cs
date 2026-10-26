using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch03.Scenarios;

// Base comune dei due casi del capitolo 3.
public abstract class Ch03Case : LabCaseBase
{
    public override int Chapter => 3;
    public override string Owasp => "OWASP A07:2025 Identification and Authentication Failures";
    public override string Iso27001 => "8.5, 5.17";
    public override string TestClass => "tests/Fortino.Tests/Ch03/Ch03AuthenticationTests.cs";
}
