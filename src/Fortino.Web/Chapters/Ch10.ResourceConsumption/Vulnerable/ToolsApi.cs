using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Fortino.Web.Chapters.Ch10.Vulnerable;

// VERSIONE VULNERABILE. Gli indirizzi veri usati dai tre casi del capitolo 10, nella versione
// senza difese. La riga pericolosa di ciascun caso sta nelle classi di aiuto (UnboundedImage,
// SlowRegex, UnboundedExport); qui ci sono solo gli indirizzi e la misura dell'effetto, che
// torna alla pagina nell'indirizzo perché il lettore la veda.
public static class ToolsApi
{
    // 10.1 Immagine con dimensioni enormi: il modulo «Controlla un'immagine» della pagina /documents.
    public static void MapCheckImage(RouteGroupBuilder group)
    {
        group.MapPost("/documents/check-image", async (IFormFile file) =>
        {
            (int width, int height) = await Ch10Header.ReadPngSizeAsync(file);
            var sw = Stopwatch.StartNew();
            long bytes = UnboundedImage.AllocateBitmap(width, height);   // alloca senza controllare
            sw.Stop();
            return Ch10Header.BackTo("/documents", new()
            {
                ["byte"] = file.Length.ToString(),
                ["larghezza"] = width.ToString(),
                ["altezza"] = height.ToString(),
                ["mb"] = (bytes / (1024 * 1024)).ToString(),
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
            bool matches = SlowRegex.IsMatch(text ?? "");   // nessun limite di tempo
            sw.Stop();
            return Ch10Header.BackTo("/documents", new()
            {
                ["caratteri"] = (text ?? "").Length.ToString(),
                ["esito"] = matches ? "Valid" : "NotValid",
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
            long size = await UnboundedExport.ExportAllInMemory(Ch10Header.Entries(count), ct);   // tutto in memoria
            sw.Stop();
            return Ch10Header.BackTo("/orders", new()
            {
                ["elementi"] = count.ToString(),
                ["kb"] = (size / 1024).ToString(),
                ["ms"] = sw.ElapsedMilliseconds.ToString(),
            });
        });
    }
}
