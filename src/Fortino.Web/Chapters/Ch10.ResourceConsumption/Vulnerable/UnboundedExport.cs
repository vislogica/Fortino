using System.IO.Compression;
using Fortino.Web.Chapters.Ch10.Secure;

namespace Fortino.Web.Chapters.Ch10.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class UnboundedExport
{
    #region libro: esportazione-vulnerabile
    // L'archivio ZIP si costruisce tutto in memoria, senza limiti al numero di elementi: un'esportazione
    // grande (o molte insieme) occupa tanta memoria quanti sono i dati esportati.
    public static async Task<long> ExportAllInMemory(IAsyncEnumerable<StreamingZipExport.ExportEntry> entries, CancellationToken ct)
    {
        // PASSO 1: l'intero archivio si costruisce dentro un MemoryStream, cioè tutto in RAM.
        using MemoryStream memory = new MemoryStream();

        using (ZipArchive archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            // PASSO 2: si aggiungono tutti gli elementi, senza un numero massimo.
            await foreach (StreamingZipExport.ExportEntry item in entries.WithCancellation(ct))
            {
                ZipArchiveEntry entry = archive.CreateEntry(item.Name, CompressionLevel.Optimal);
                await using Stream entryStream = entry.Open();
                await using StreamWriter writer = new StreamWriter(entryStream);
                await writer.WriteAsync(item.Content);
            }
        }

        // PASSO 3: la memoria occupata è grande quanto l'intero archivio.
        long sizeInMemory = memory.Length;
        return sizeInMemory;
    }
    #endregion
}
