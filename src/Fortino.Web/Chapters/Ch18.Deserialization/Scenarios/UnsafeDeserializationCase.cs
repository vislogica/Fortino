using Fortino.Web.Chapters.Ch18.Vulnerable;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch18.Scenarios;

// Caso 18.2: i dati scelgono quale tipo viene creato durante la deserializzazione.
public sealed class UnsafeDeserializationCase : Ch18Case
{
    public override string Slug => "unsafe-deserialization";
    public override IReadOnlyList<string> LabUsers => ["anna"];
    public override string Title => "Il tipo lo scelgono i dati";
    public override string Summary =>
        "Un JSON con un campo di tipo fa creare al server una classe scelta dall'attaccante. La versione corretta deserializza sempre in un tipo fisso.";
    public override string BookReference => "capitolo 18, caso 18.2";
    public override CodeLocation VulnerableCode => new("Ch18.Deserialization/Vulnerable/UnsafeDeserialization.cs", "deserializzazione-vulnerabile");
    public override CodeLocation SecureCode => new("Ch18.Deserialization/Secure/SafeDeserialization.cs", "deserializzazione-corretta");

    // Un JSON che chiede di creare DangerousType, invece dell'ordine atteso.
    private static readonly string Payload =
        $$"""{"$type":"{{typeof(DangerousType).FullName}}, Fortino.Web"}""";

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);
        DangerousType.WasCreated = false;

        var client = await context.Clients.CreateAsync(LabUsers[0], ct);
        run.Log("Viene inviato un JSON che, con un campo \"$type\", chiede di creare una classe scelta da chi attacca.");

        using var content = new StringContent(Payload, System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"{Api(variant)}/import/json", content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        run.Log($"Risposta del server: {(int)response.StatusCode}. Tipo creato: {body}.");

        if (DangerousType.WasCreated)
            run.Succeeded("Il server ha creato la classe scelta dall'attaccante (DangerousType). Con un tipo " +
                          "costruito ad arte, qui si eseguirebbe codice: è la deserializzazione insicura.");
        else
            run.Blocked("Il server ha deserializzato nel tipo fisso previsto: i dati non hanno potuto scegliere la classe.");
    }
}
