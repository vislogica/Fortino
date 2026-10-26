using System.IO.Compression;

namespace Fortino.Web.Chapters.Ch10.Secure;

// VERSIONE CORRETTA (caso 10.3).
// Un'esportazione che costruisce l'intero archivio ZIP in memoria consuma tanta memoria quanti
// sono i dati esportati: con un export grande, pochi utenti insieme esauriscono il server. La
// versione corretta scrive l'archivio direttamente sul flusso di uscita, un elemento alla volta,
// e pone un limite al numero di elementi.
public static class StreamingZipExport
{
    public const int MaxEntries = 10_000;

    public sealed record ExportEntry(string Name, string Content);

    #region libro: esportazione-corretta
    // Scrive lo ZIP direttamente su 'output' mentre legge gli elementi: in memoria sta sempre
    // un solo elemento alla volta, non l'intero archivio.
    public static async Task WriteAsync(Stream output, IAsyncEnumerable<ExportEntry> entries, CancellationToken ct)
    {
        // PASSO 1: l'archivio ZIP scrive direttamente sul flusso di uscita, non in memoria.
        using ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        int count = 0;

        // PASSO 2: si leggono gli elementi uno alla volta, man mano che arrivano.
        await foreach (ExportEntry item in entries.WithCancellation(ct))
        {
            // PASSO 3: oltre il numero massimo di elementi ci si ferma.
            count = count + 1;
            bool tooManyEntries = count > MaxEntries;
            if (tooManyEntries)
            {
                throw new InvalidOperationException($"Esportazione troppo grande: oltre {MaxEntries} elementi.");
            }

            // PASSO 4: si scrive l'elemento nello ZIP; va subito sul flusso di uscita e la memoria
            // si libera prima di passare al successivo.
            ZipArchiveEntry entry = archive.CreateEntry(item.Name, CompressionLevel.Optimal);
            await using Stream entryStream = entry.Open();
            await using StreamWriter writer = new StreamWriter(entryStream);
            await writer.WriteAsync(item.Content.AsMemory(), ct);
        }
    }
    #endregion
}
