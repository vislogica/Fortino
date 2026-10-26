using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch01.Scenarios;

// Base comune dei due casi del capitolo 1.
public abstract class Ch01Case : LabCaseBase
{
    public override int Chapter => 1;
    public override string Owasp => "OWASP A01:2025 Broken Access Control";
    public override string Iso27001 => "5.15, 8.3";
    public override string TestClass => "tests/Fortino.Tests/Ch01/Ch01AccessControlTests.cs";
}
