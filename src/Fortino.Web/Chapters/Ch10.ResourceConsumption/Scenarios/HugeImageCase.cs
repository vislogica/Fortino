using Fortino.Web.Chapters.Ch10.Secure;
using Fortino.Web.Chapters.Ch10.Vulnerable;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch10.Scenarios;

// Caso 10.1: un file piccolo dichiara dimensioni enormi e fa allocare al server tanta memoria.
public sealed class HugeImageCase : Ch10Case
{
    public override string Slug => "huge-image";
    public override string Title => "Immagine con dimensioni enormi";
    public override string Summary =>
        "Un file di pochi byte che dichiara un'immagine enorme fa allocare al server una grande quantità di memoria. La versione corretta controlla le dimensioni prima di caricare.";
    public override string BookReference => "capitolo 10, caso 10.1";
    public override CodeLocation VulnerableCode => new("Ch10.ResourceConsumption/Vulnerable/UnboundedImage.cs", "immagine-vulnerabile");
    public override CodeLocation SecureCode => new("Ch10.ResourceConsumption/Secure/ImageSizeGuard.cs", "limite-immagine-corretto");

    private const int Width = 5000;    // oltre il limite della versione corretta (4000)
    private const int Height = 5000;

    public override async Task RunAsync(CodeVariant variant, AttackRun run, LabContext context, CancellationToken ct)
    {
        await context.ResetDataAsync(run, ct);

        // Un'intestazione PNG di 24 byte che dichiara un'immagine di 5000 × 5000 pixel.
        var header = PngHeader(Width, Height);
        run.Log($"Viene inviato un file immagine di appena {header.Length} byte, che però dichiara {Width} × {Height} pixel.");

        if (variant == CodeVariant.Vulnerable)
        {
            var allocated = UnboundedImage.AllocateBitmap(Width, Height);
            run.Log($"Il server non controlla le dimensioni e prepara la bitmap: {allocated / (1024 * 1024)} MB allocati da {header.Length} byte di input.");
            run.Succeeded($"Da un file di {header.Length} byte il server ha allocato {allocated / (1024 * 1024)} MB. " +
                          "Pochi file del genere insieme esauriscono la memoria del server.");
        }
        else
        {
            var result = ImageSizeGuard.Check(new MemoryStream(header), header.Length);
            run.Log($"Il server controlla le dimensioni dichiarate prima di caricare: esito {result}.");
            if (result == ImageSizeGuard.Result.Ok)
                run.Succeeded("L'immagine enorme è stata accettata: il controllo non ha funzionato.");
            else
                run.Blocked("L'immagine è stata rifiutata per le dimensioni dichiarate, senza allocare nulla.");
        }
    }

    // Firma PNG (8 byte) + blocco IHDR con larghezza e altezza (byte 16-23), in tutto 24 byte.
    private static byte[] PngHeader(int width, int height)
    {
        var bytes = new byte[24];
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        signature.CopyTo(bytes, 0);
        WriteBigEndian(bytes, 16, width);
        WriteBigEndian(bytes, 20, height);
        return bytes;
    }

    private static void WriteBigEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
