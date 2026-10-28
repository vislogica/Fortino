using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch13.Scenarios;

// Base comune dei due casi del capitolo 13.
public abstract class Ch13Case : LabCaseBase
{
    public override int Chapter => 13;
    public override string Owasp => "OWASP A10:2025 Mishandling of Exceptional Conditions · A02:2025 Security Misconfiguration";
    public override string Iso27001 => "8.26, 8.27";
    public override string TestClass => "tests/Fortino.Tests/Ch13/Ch13ErrorHandlingTests.cs";
}
