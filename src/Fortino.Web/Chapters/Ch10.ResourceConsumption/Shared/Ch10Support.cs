using Fortino.Web.Chapters.Ch10.Secure;

namespace Fortino.Web.Chapters.Ch10;

// Aiuti condivisi dai tre casi del capitolo 10: non sono codice mostrato nel libro, solo il
// contorno degli indirizzi (leggere le dimensioni dichiarate, il corpo della richiesta, gli elementi).
public static class Ch10Header
{
    // Il corpo della richiesta di validazione del testo (caso 10.2).
    public sealed record ValidateRequest(string? Text);

    // Legge larghezza e altezza DICHIARATE nell'intestazione PNG (24 byte: firma + blocco IHDR),
    // senza caricare l'immagine. Se il file è più corto, restituisce (0, 0).
    public static async Task<(int Width, int Height)> ReadPngSizeAsync(IFormFile file)
    {
        byte[] buffer = new byte[24];
        await using Stream stream = file.OpenReadStream();
        int read = 0;
        while (read < 24)
        {
            int n = await stream.ReadAsync(buffer.AsMemory(read, 24 - read));
            if (n == 0) break;
            read += n;
        }
        if (read < 24) return (0, 0);
        int width = ReadBigEndian(buffer, 16);
        int height = ReadBigEndian(buffer, 20);
        return (width, height);
    }

    private static int ReadBigEndian(byte[] buffer, int start) =>
        (buffer[start] << 24) | (buffer[start + 1] << 16) | (buffer[start + 2] << 8) | buffer[start + 3];

    // Gli elementi di un'esportazione (caso 10.3): generati uno alla volta, senza tenerli tutti.
    public static async IAsyncEnumerable<StreamingZipExport.ExportEntry> Entries(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await Task.Yield();
            yield return new StreamingZipExport.ExportEntry($"documento-{i}.txt", $"contenuto del documento numero {i}");
        }
    }
}
