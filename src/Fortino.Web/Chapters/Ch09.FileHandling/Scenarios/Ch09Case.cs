using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch09.Scenarios;

// Base comune dei due casi del capitolo 9.
public abstract class Ch09Case : LabCaseBase
{
    public override int Chapter => 9;
    public override string Owasp => "OWASP A01:2025 Broken Access Control";
    public override string Iso27001 => "8.26, 8.27";
    public override string TestClass => "tests/Fortino.Tests/Ch09/Ch09FileHandlingTests.cs";
}
