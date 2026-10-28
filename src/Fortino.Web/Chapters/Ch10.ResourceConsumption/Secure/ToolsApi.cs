using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Chapters.Ch10.Secure;

// VERSIONE CORRETTA. Gli stessi indirizzi della versione vulnerabile, protetti dalle difese del
// capitolo 10 (controllo delle dimensioni dichiarate, limite di lunghezza più tempo massimo, limite
// al numero di elementi). La riga che conta sta nelle classi ImageSizeGuard, SafeTextValidator e
// StreamingZipExport; qui c'è l'indirizzo che le usa.
public static class ToolsApi
{
    // 10.1 Immagine con dimensioni enormi: il modulo «Controlla un'immagine» della pagina /documents.
    public static void MapCheckImage(RouteGroupBuilder group)
    {
        group.MapPost("/documents/check-image", async (IFormFile file) =>
        {
            (int width, int height) = await Ch10Header.ReadPngSizeAsync(file);
            var sw = Stopwatch.StartNew();
            using var header = new MemoryStream();
            await file.CopyToAsync(header);
            header.Position = 0;
            ImageSizeGuard.Result result = ImageSizeGuard.Check(header, file.Length);   // controlla, non alloca
            sw.Stop();
            return Ch10Header.BackTo("/documents", new()
            {
                ["byte"] = file.Length.ToString(),
                ["larghezza"] = width.ToString(),
                ["altezza"] = height.ToString(),
                ["controllo"] = result.ToString(),
                ["ms"] = sw.ElapsedMilliseconds.ToString(),
            });
        });
    }

    // 10.2 Espressione regolare lenta (ReDoS): il modulo «Controlla un testo» della pagina /documents.
    public static void MapValidate(RouteGroupBuilder group)
    {
        group.MapPost("/notes/validate", ([FromForm] string? text) =>
        {
            var sw = Stopwatch.StartNew();
            SafeTextValidator.Outcome outcome = SafeTextValidator.Check(text);   // limite + tempo massimo
            sw.Stop();
            return Ch10Header.BackTo("/documents", new()
            {
                ["caratteri"] = (text ?? "").Length.ToString(),
                ["esito"] = outcome.ToString(),
                ["ms"] = sw.ElapsedMilliseconds.ToString(),
            });
        });
    }

    // 10.3 Esportazione senza limiti: il modulo «Esporta in ZIP» della pagina /orders, GET /orders/export?count=N
    public static void MapExport(RouteGroupBuilder group)
    {
        group.MapGet("/orders/export", async (int count, CancellationToken ct) =>
        {
            var sw = Stopwatch.StartNew();
            await using var output = new MemoryStream();
            try
            {
                await StreamingZipExport.WriteAsync(output, Ch10Header.Entries(count), ct);   // si ferma al limite
            }
            catch (InvalidOperationException ex)
            {
                return Ch10Header.BackTo("/orders", new() { ["elementi"] = count.ToString(), ["errore"] = ex.Message });
            }
            sw.Stop();
            return Ch10Header.BackTo("/orders", new()
            {
                ["elementi"] = count.ToString(),
                ["kb"] = (output.Length / 1024).ToString(),
                ["ms"] = sw.ElapsedMilliseconds.ToString(),
            });
        });
    }
}
