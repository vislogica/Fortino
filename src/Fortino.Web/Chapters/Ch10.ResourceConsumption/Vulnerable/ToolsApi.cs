using System.Diagnostics;
using Fortino.Web.Chapters.Ch10.Secure;

namespace Fortino.Web.Chapters.Ch10.Vulnerable;

// VERSIONE VULNERABILE. Gli indirizzi veri usati dai tre casi del capitolo 10, nella versione
// senza difese. La riga pericolosa di ciascun caso sta nelle classi di aiuto (UnboundedImage,
// SlowRegex, UnboundedExport); qui ci sono solo gli indirizzi e la misura dell'effetto.
public static class ToolsApi
{
    // 10.1 Immagine con dimensioni enormi: POST /documents/check-image
    public static void MapCheckImage(RouteGroupBuilder group)
    {
        group.MapPost("/documents/check-image", async (IFormFile file) =>
        {
            (int width, int height) = await Ch10Header.ReadPngSizeAsync(file);
            var sw = Stopwatch.StartNew();
            long bytes = UnboundedImage.AllocateBitmap(width, height);   // alloca senza controllare
            sw.Stop();
            return Results.Ok(new
            {
                fileBytes = file.Length,
                declaredWidth = width,
                declaredHeight = height,
                allocatedMB = bytes / (1024 * 1024),
                ms = sw.ElapsedMilliseconds,
            });
        }).DisableAntiforgery();
    }

    // 10.2 Espressione regolare lenta (ReDoS): POST /notes/validate
    public static void MapValidate(RouteGroupBuilder group)
    {
        group.MapPost("/notes/validate", (Ch10Header.ValidateRequest body) =>
        {
            var sw = Stopwatch.StartNew();
            bool matches = SlowRegex.IsMatch(body.Text ?? "");   // nessun limite di tempo
            sw.Stop();
            return Results.Ok(new { length = (body.Text ?? "").Length, matches, ms = sw.ElapsedMilliseconds });
        }).DisableAntiforgery();
    }

    // 10.3 Esportazione senza limiti: GET /orders/export?count=N
    public static void MapExport(RouteGroupBuilder group)
    {
        group.MapGet("/orders/export", async (int count, CancellationToken ct) =>
        {
            var sw = Stopwatch.StartNew();
            long size = await UnboundedExport.ExportAllInMemory(Ch10Header.Entries(count), ct);   // tutto in memoria
            sw.Stop();
            return Results.Ok(new { requested = count, sizeKB = size / 1024, ms = sw.ElapsedMilliseconds });
        });
    }
}
