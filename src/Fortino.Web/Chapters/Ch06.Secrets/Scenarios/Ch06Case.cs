using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch06.Scenarios;

// Base comune dei due casi del capitolo 6.
public abstract class Ch06Case : LabCaseBase
{
    public override int Chapter => 6;
    public override string Owasp => "OWASP A04:2025 Cryptographic Failures · A02:2025 Security Misconfiguration";
    public override string Iso27001 => "8.24, 5.17";
    public override string TestClass => "tests/Fortino.Tests/Ch06/Ch06SecretsTests.cs";
}
