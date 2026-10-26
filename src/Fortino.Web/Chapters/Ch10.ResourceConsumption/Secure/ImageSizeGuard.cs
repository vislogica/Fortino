namespace Fortino.Web.Chapters.Ch10.Secure;

// VERSIONE CORRETTA (caso 10.1).
// Prima di accettare un'immagine, si controllano le sue dimensioni dichiarate leggendo solo
// l'intestazione del file, senza caricarla né decodificarla in memoria. Un'immagine che dichiara
// milioni di pixel verrebbe rifiutata qui, prima di consumare memoria.
public static class ImageSizeGuard
{
    public const int MaxWidth = 4000;
    public const int MaxHeight = 4000;
    public const long MaxBytes = 5 * 1024 * 1024;

    public enum Result { Ok, TooManyBytes, NotAnImage, TooManyPixels }

    #region libro: limite-immagine-corretto
    // Controlla un'immagine PNG senza decodificarla: legge larghezza e altezza dai 24 byte
    // iniziali (la firma PNG più il blocco IHDR) e le confronta con il limite.
    public static Result Check(Stream header, long declaredBytes)
    {
        // PASSO 1: un file troppo grande si rifiuta subito.
        if (declaredBytes > MaxBytes)
        {
            return Result.TooManyBytes;
        }

        // PASSO 2: si leggono solo i primi 24 byte del file, non l'immagine intera.
        byte[] buffer = new byte[24];
        int bytesRead = header.Read(buffer, 0, 24);
        if (bytesRead < 24)
        {
            return Result.NotAnImage;
        }
        if (!IsPng(buffer))
        {
            return Result.NotAnImage;
        }

        // PASSO 3: larghezza e altezza stanno nei byte 16-23 dell'intestazione.
        long width = ReadBigEndian(buffer, 16);
        long height = ReadBigEndian(buffer, 20);

        // PASSO 4: dimensioni assurde o oltre il limite: si rifiuta, prima di allocare memoria.
        bool sizeIsValid = width > 0 && height > 0 && width <= MaxWidth && height <= MaxHeight;
        if (!sizeIsValid)
        {
            return Result.TooManyPixels;
        }

        return Result.Ok;
    }

    private static bool IsPng(byte[] header)
    {
        // Un file PNG comincia sempre con i byte 89 50 4E 47 (la sigla "PNG").
        bool isPng = header[0] == 0x89
                  && header[1] == 0x50
                  && header[2] == 0x4E
                  && header[3] == 0x47;
        return isPng;
    }

    private static long ReadBigEndian(byte[] buffer, int start)
    {
        // Quattro byte in fila formano un numero; il primo è il più significativo (big-endian).
        long byte0 = buffer[start];
        long byte1 = buffer[start + 1];
        long byte2 = buffer[start + 2];
        long byte3 = buffer[start + 3];
        long value = (byte0 << 24) | (byte1 << 16) | (byte2 << 8) | byte3;
        return value;
    }
    #endregion
}
