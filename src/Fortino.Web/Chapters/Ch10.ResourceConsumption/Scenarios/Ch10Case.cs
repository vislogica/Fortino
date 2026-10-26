using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch10.Scenarios;

// Base comune dei tre casi del capitolo 10.
public abstract class Ch10Case : LabCaseBase
{
    public override int Chapter => 10;
    public override string Owasp => "OWASP A04:2025 Insecure Design / Unrestricted Resource Consumption";
    public override string Iso27001 => "8.6, 8.26";
    public override string TestClass => "tests/Fortino.Tests/Ch10/Ch10ResourceConsumptionTests.cs";
}
