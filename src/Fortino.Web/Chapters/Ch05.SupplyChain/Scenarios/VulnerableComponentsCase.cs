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
    public override string Owasp => "OWASP A03:2025 Software Supply Chain Failures";
    public override string Iso27001 => "8.8, 5.21";
    public override string TestClass => "—";
    public override CodeLocation VulnerableCode => new("Ch05.SupplyChain/Vulnerable/NotIncludedYet.cs", "componenti-vulnerabili-da-integrare");
    public override CodeLocation SecureCode => new("Ch05.SupplyChain/Secure/DependencyHygiene.cs", "controllo-dipendenze");

    public override bool VulnerableAvailable => false;

    public override bool IsProcess => true;

    // Il comando di controllo, preso dall'unica fonte vera: la costante della versione corretta.
    public override string? ProcessCommand => Secure.DependencyHygiene.CheckVulnerable;

    // I passi della prova, gli stessi del libro (capitolo 5).
    public override IReadOnlyList<string> ProcessSteps =>
    [
        "Apri un terminale nella cartella samples/Ch05.SupplyChain del progetto (in Visual Studio: tasto destro sul progetto VulnerableSample, poi «Apri nel terminale»). Non serve avviare il progetto.",
        "Esegui il controllo: dotnet list package --vulnerable --include-transitive. Compare System.Text.Encodings.Web 4.7.1, con gravità Critical e il collegamento all'avviso.",
        "Vedi qual è la versione aggiornata: dotnet list package --outdated. La colonna «Più recenti» indica l'ultima disponibile.",
        "Correggi: apri VulnerableSample.csproj e cambia Version=\"4.7.1\" con la versione più recente.",
        "Rilancia il primo comando: ora dice che il progetto non contiene pacchetti vulnerabili.",
        "Rimetti la versione 4.7.1 se vuoi ripetere la prova da capo.",
    ];

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
