using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch18.Scenarios;

// Base comune dei due casi del capitolo 18.
public abstract class Ch18Case : LabCaseBase
{
    public override int Chapter => 18;
    public override string Owasp => "OWASP A08:2025 Software or Data Integrity Failures · A02:2025 Security Misconfiguration (XXE)";
    public override string Iso27001 => "8.26";
    public override string TestClass => "tests/Fortino.Tests/Ch18/Ch18DeserializationTests.cs";
}
