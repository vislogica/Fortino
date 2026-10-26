namespace Fortino.Web.Chapters.Ch10.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class UnboundedImage
{
    #region libro: immagine-vulnerabile
    // Nessun controllo sulle dimensioni dichiarate: si prepara in memoria la bitmap della misura
    // indicata nel file. Un file di pochi byte che dichiara dimensioni enormi fa allocare al server
    // una quantità di memoria spropositata (è la "bomba di decompressione").
    public static long AllocateBitmap(int declaredWidth, int declaredHeight)
    {
        // PASSO 1: memoria necessaria: 4 byte per ogni pixel dichiarato nel file.
        long pixelCount = (long)declaredWidth * declaredHeight;
        long bytes = pixelCount * 4;

        // PASSO 2: si alloca tutta quella memoria, senza chiedersi se le dimensioni hanno senso.
        byte[] bitmap = new byte[bytes];
        return bitmap.LongLength;
    }
    #endregion
}
