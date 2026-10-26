namespace Fortino.Web.Chapters.Ch09.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Salva e legge i file unendo la cartella dei documenti al nome scelto dall'utente, senza alcun
// controllo. Un nome che risale di cartella porta il file, o la lettura, fuori dall'area prevista.
public static class UnsafeDocumentIO
{
    #region libro: salva-file-vulnerabile
    // Il percorso si costruisce con il nome ricevuto, così com'è: se il nome risale di cartella,
    // il file viene scritto fuori dalla cartella dei documenti.
    public static string SaveByName(DocumentStore store, string fileName, byte[] content)
    {
        // PASSO 1: si unisce la cartella dei documenti al nome scelto dall'utente.
        // Se fileName vale  ../database.db  il percorso esce dalla cartella dei documenti.
        string path = Path.Combine(store.Root, fileName);

        // PASSO 2: si scrive il file in quel percorso, qualunque sia.
        File.WriteAllBytes(path, content);

        // PASSO 3: si restituisce il percorso completo dove è finito il file.
        string fullPath = Path.GetFullPath(path);
        return fullPath;
    }
    #endregion

    #region libro: scarica-file-vulnerabile
    // Specularmente, in lettura: il nome chiesto dall'utente decide quale file si apre. Un nome che
    // risale di cartella fa leggere un file che sta fuori dalla cartella dei documenti.
    public static byte[] ReadByName(DocumentStore store, string fileName)
    {
        // PASSO 1: si unisce la cartella dei documenti al nome chiesto dall'utente.
        string path = Path.Combine(store.Root, fileName);

        // PASSO 2: si legge il file in quel percorso, qualunque sia.
        byte[] content = File.ReadAllBytes(path);
        return content;
    }
    #endregion
}
