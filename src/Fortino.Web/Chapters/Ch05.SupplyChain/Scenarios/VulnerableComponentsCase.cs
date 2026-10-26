using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch05.Scenarios;

// Caso 5.1: componenti con vulnerabilità note. Per sua natura non è un attacco da eseguire nel
// Laboratorio, ma un controllo da fare con un comando: qui si mostra la versione corretta (il processo).
public sealed class VulnerableComponentsCase : LabCaseBase
{
    public override int Chapter => 5;
    public override string Slug => "vulnerable-components";
    public override string Title => "Componenti con vulnerabilità note";
    public override string Summary =>
        "Il rischio non è un difetto del tuo codice, ma un pacchetto che usi con una vulnerabilità nota. Si scopre con un comando e si evita tenendo tutto aggiornato.";
    public override string BookReference => "capitolo 5";
    public override string Owasp => "OWASP A06:2025 Vulnerable and Outdated Components";
    public override string Iso27001 => "8.8, 5.21";
    public override string TestClass => "—";
    public override CodeLocation VulnerableCode => new("Ch05.SupplyChain/Vulnerable/NotIncludedYet.cs", "componenti-vulnerabili-da-integrare");
    public override CodeLocation SecureCode => new("Ch05.SupplyChain/Secure/DependencyHygiene.cs", "controllo-dipendenze");

    public override bool VulnerableAvailable => false;

    public override Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        run.Log("Questo capitolo non ha un attacco da eseguire: la vulnerabilità sta in un pacchetto, non nel codice di Fortino.");
        run.Log("Il controllo si fa dalla riga di comando, nella cartella del progetto:");
        run.Log("dotnet list package --vulnerable --include-transitive");
        run.Blocked("Il rischio dei componenti si gestisce con un controllo periodico e gli aggiornamenti. " +
                    "Vedi il capitolo 5 del libro; il codice a fianco raccoglie i comandi e le impostazioni utili.");
        return Task.CompletedTask;
    }
}
