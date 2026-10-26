using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch12.Scenarios;

// Base comune dei due casi del capitolo 12.
public abstract class Ch12Case : LabCaseBase
{
    public override int Chapter => 12;
    public override string Owasp => "OWASP A02:2025 Cryptographic Failures";
    public override string Iso27001 => "8.24";
    public override string TestClass => "tests/Fortino.Tests/Ch12/Ch12WeakCryptographyTests.cs";
}
