using Fortino.Web.Chapters.Ch10.Secure;

namespace Fortino.Web.Chapters.Ch10;

// Aiuti condivisi dai tre casi del capitolo 10: non sono codice mostrato nel libro, solo il
// contorno degli indirizzi (leggere le dimensioni dichiarate, il corpo della richiesta, gli elementi).
public static class Ch10Header
{
    // Torna alla pagina con la misura dell'effetto nell'indirizzo (memoria, tempo, dimensione):
    // la pagina la mostra al lettore.
    public static IResult BackTo(string page, Dictionary<string, string> values)
    {
        string query = string.Join("&", values.Select(v => $"{v.Key}={Uri.EscapeDataString(v.Value)}"));
        return Results.LocalRedirect($"{page}?{query}");
    }

    // Il file del caso 10.1: solo l'intestazione di un PNG (24 byte: firma più blocco IHDR) che
    // dichiara width × height pixel. Non è un'immagine vera: è tutto quello che serve all'attacco.
    public static byte[] PngHeader(int width, int height)
    {
        byte[] bytes = new byte[24];
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        signature.CopyTo(bytes, 0);
        byte[] ihdr = [0x00, 0x00, 0x00, 0x0D, (byte)'I', (byte)'H', (byte)'D', (byte)'R'];
        ihdr.CopyTo(bytes, 8);
        WriteBigEndian(bytes, 16, width);
        WriteBigEndian(bytes, 20, height);
        return bytes;
    }

    private static void WriteBigEndian(byte[] bytes, int start, int value)
    {
        bytes[start] = (byte)(value >> 24);
        bytes[start + 1] = (byte)(value >> 16);
        bytes[start + 2] = (byte)(value >> 8);
        bytes[start + 3] = (byte)value;
    }

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
