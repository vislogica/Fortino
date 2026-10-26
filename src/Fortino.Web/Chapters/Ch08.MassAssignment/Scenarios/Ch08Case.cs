using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch08.Scenarios;

// Base comune dei casi del capitolo 8.
public abstract class Ch08Case : LabCaseBase
{
    public override int Chapter => 8;
    public override string Owasp => "OWASP API3:2023 Broken Object Property Level Authorization";
    public override string Iso27001 => "8.28";
    public override string TestClass => "tests/Fortino.Tests/Ch08/Ch08MassAssignmentTests.cs";
}
