using Fortino.Web.Data;
using Microsoft.Data.Sqlite;

namespace Fortino.Web.Chapters.Ch09;

// La cartella dei documenti caricati: App_Data/documents, accanto al database.
// Il Laboratorio la svuota a ogni "Ripristina i dati".
public sealed class DocumentStore
{
    public DocumentStore(DbConnectionFactory factory)
    {
        Root = RootFor(factory.ConnectionString);
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    // La cartella che contiene il database (App_Data): i casi del capitolo 9 controllano
    // che nessun file arrivi o venga letto da qui.
    public string DataFolder => Path.GetDirectoryName(Root)!;

    public static string RootFor(string connectionString)
    {
        // PASSO 1: dalla stringa di connessione si ricava il file del database.
        SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder(connectionString);
        string databaseFile = builder.DataSource;

        // PASSO 2: la cartella dei documenti sta accanto al database.
        string databaseFolder = Path.GetFullPath(Path.GetDirectoryName(databaseFile)!);
        string documentsFolder = Path.Combine(databaseFolder, "documents");
        return documentsFolder;
    }

    #region libro: percorso-controllato
    // Ultima difesa: il percorso finale, risolto da Path.GetFullPath, deve stare dentro Root.
    public string PathFor(string storedName)
    {
        // PASSO 1: la cartella dei documenti, in forma completa e con la barra finale.
        string root = Path.GetFullPath(Root) + Path.DirectorySeparatorChar;

        // PASSO 2: si unisce il nome alla cartella e si risolve il percorso: GetFullPath elimina
        // i "..", così si vede dove punta davvero.
        string combined = Path.Combine(root, storedName);
        string full = Path.GetFullPath(combined);

        // PASSO 3: se il percorso finale non sta dentro la cartella dei documenti, ci si ferma.
        bool isInsideRoot = full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        if (!isInsideRoot)
        {
            throw new InvalidOperationException("Percorso fuori dalla cartella dei documenti.");
        }
        return full;
    }
    #endregion
}
