using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch18.Scenarios;

// Caso 18.1: un XML con una definizione di tipo documento (DTD) ed entità viene elaborato dal
// lettore permissivo. È il meccanismo alla base dell'XXE: una volta che la DTD viene elaborata,
// un'entità può puntare a un file o a un indirizzo interni (il testo del capitolo lo spiega).
public sealed class XxeCase : Ch18Case
{
    public override string Slug => "xxe";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "XML che porta una sua definizione";
    public override string Summary =>
        "Un XML con una DTD ed entità viene elaborato dalla versione vulnerabile: il valore arriva dalla DTD, non dai dati. La versione corretta rifiuta la DTD.";
    public override string BookReference => "capitolo 18, caso 18.1";
    public override CodeLocation VulnerableCode => new("Ch18.Deserialization/Vulnerable/UnsafeXmlImport.cs", "xml-vulnerabile");
    public override CodeLocation SecureCode => new("Ch18.Deserialization/Secure/SafeXmlImport.cs", "xml-corretto");

    private const string Marker = "VALORE-DALLA-DTD";

    // Un XML che definisce un'entità nella DTD e la usa nel nome.
    private static readonly string Payload =
        $"<?xml version=\"1.0\"?><!DOCTYPE cliente [ <!ENTITY iniettata \"{Marker}\"> ]>" +
        "<cliente><nome>&iniettata;</nome></cliente>";

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log("Viene inviato un XML che porta con sé una DTD con un'entità: il nome del cliente non è scritto " +
                "direttamente, ma viene dall'entità definita nella DTD.");

        using var content = new StringContent(Payload, System.Text.Encoding.UTF8, "application/xml");
        using var response = await client.PostAsync($"{Api(variant)}/import/xml", content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        run.Log($"Risposta del server: {(int)response.StatusCode}. Corpo: {body}.");

        if (response.IsSuccessStatusCode && body.Contains(Marker, StringComparison.Ordinal))
            run.Succeeded("Il lettore ha elaborato la DTD ed espanso l'entità. Da qui, un'entità che punta a un " +
                          "file o a un indirizzo interno li farebbe leggere al server: è l'attacco XXE.");
        else
            run.Blocked("Il lettore ha rifiutato la DTD: le entità non vengono elaborate, quindi l'XXE è impossibile.");
    }
}
