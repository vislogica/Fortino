using Fortino.Web.Chapters.Ch10.Secure;
using Fortino.Web.Chapters.Ch10.Vulnerable;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch10.Scenarios;

// Caso 10.3: un'esportazione senza limiti costruisce l'intero archivio in memoria.
public sealed class UnboundedExportCase : Ch10Case
{
    public override string Slug => "unbounded-export";
    public override string Title => "Esportazione senza limiti";
    public override string Summary =>
        "Un'esportazione di moltissimi elementi viene costruita tutta in memoria, senza limiti. La versione corretta si ferma oltre un numero massimo di elementi.";
    public override string BookReference => "capitolo 10, caso 10.3";
    public override CodeLocation VulnerableCode => new("Ch10.ResourceConsumption/Vulnerable/UnboundedExport.cs", "esportazione-vulnerabile");
    public override CodeLocation SecureCode => new("Ch10.ResourceConsumption/Secure/StreamingZipExport.cs", "esportazione-corretta");

    private static readonly int Requested = StreamingZipExport.MaxEntries + 1;   // oltre il limite della versione corretta

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);
        run.Log($"Viene chiesta un'esportazione di {Requested} elementi (oltre il limite di {StreamingZipExport.MaxEntries}).");

        if (variant == CodeVariant.Vulnerable)
        {
            var size = await UnboundedExport.ExportAllInMemory(Entries(Requested), ct);
            run.Log($"Il server ha costruito l'intero archivio in memoria: {size / 1024} KB per {Requested} elementi.");
            run.Succeeded($"Nessun limite: {Requested} elementi costruiti tutti in memoria. Con elementi più grandi, " +
                          "o molte esportazioni insieme, la memoria del server finisce.");
        }
        else
        {
            await using var output = new MemoryStream();
            try
            {
                await StreamingZipExport.WriteAsync(output, Entries(Requested), ct);
                run.Succeeded("L'esportazione enorme è stata completata: il limite non ha funzionato.");
            }
            catch (InvalidOperationException ex)
            {
                run.Log($"Il server si è fermato: \"{ex.Message}\".");
                run.Blocked("L'esportazione si è fermata al limite di elementi, senza costruire tutto in memoria.");
            }
        }
    }

    private static async IAsyncEnumerable<StreamingZipExport.ExportEntry> Entries(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await Task.Yield();
            yield return new StreamingZipExport.ExportEntry($"documento-{i}.txt", $"contenuto del documento numero {i}");
        }
    }
}
