using System.Diagnostics;

namespace Fortino.Web.Chapters.Ch10.Secure;

// VERSIONE CORRETTA. Gli stessi indirizzi della versione vulnerabile, protetti dalle difese del
// capitolo 10 (controllo delle dimensioni dichiarate, limite di lunghezza più tempo massimo, limite
// al numero di elementi). La riga che conta sta nelle classi ImageSizeGuard, SafeTextValidator e
// StreamingZipExport; qui c'è l'indirizzo che le usa.
public static class ToolsApi
{
    // 10.1 Immagine con dimensioni enormi: POST /documents/check-image
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
            return Results.Ok(new
            {
                fileBytes = file.Length,
                declaredWidth = width,
                declaredHeight = height,
                accepted = result == ImageSizeGuard.Result.Ok,
                result = result.ToString(),
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
            SafeTextValidator.Outcome outcome = SafeTextValidator.Check(body.Text);   // limite + tempo massimo
            sw.Stop();
            return Results.Ok(new { length = (body.Text ?? "").Length, outcome = outcome.ToString(), ms = sw.ElapsedMilliseconds });
        }).DisableAntiforgery();
    }

    // 10.3 Esportazione senza limiti: GET /orders/export?count=N
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
                sw.Stop();
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            sw.Stop();
            return Results.Ok(new { requested = count, sizeKB = output.Length / 1024, ms = sw.ElapsedMilliseconds });
        });
    }
}
