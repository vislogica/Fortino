namespace Fortino.Web.Chapters.Ch18.Vulnerable;

// Una classe che un attaccante può far creare di nascosto tramite la deserializzazione insicura.
// Qui l'effetto è innocuo ma visibile: il costruttore fa quello che fa per primo il codice di un
// attaccante, cioè scopre dove sta girando e lascia una traccia sul disco del server. In un attacco
// reale, al posto di queste righe, partirebbe un comando dannoso.
public sealed class DangerousType
{
    // Il file che lascia sul server: lo stesso nome, accanto al database, a ogni importazione.
    public const string EvidenceFileName = "creato-da-dangeroustype.txt";

    // La cartella del database (App_Data): la imposta il modulo del capitolo 18 all'avvio.
    public static string EvidenceFolder { get; set; } = AppContext.BaseDirectory;

    // Quello che il costruttore ha scoperto l'ultima volta, per mostrarlo nella pagina.
    public static DangerousTypeReport? LastReport { get; private set; }

    public DangerousType()
    {
        // Metti qui un breakpoint: con la versione vulnerabile il debugger si ferma, perché è
        // il JSON dell'attaccante a far costruire questa classe.

        // PASSO 1: il codice scopre dove sta girando: computer, utente e cartella del programma.
        string computer = Environment.MachineName;
        string user = Environment.UserName;
        string folder = AppContext.BaseDirectory;

        // PASSO 2: lascia un file sul disco del server, accanto al database.
        string evidencePath = Path.Combine(EvidenceFolder, EvidenceFileName);
        string evidence =
            "File creato dal costruttore di DangerousType (Laboratorio di Fortino, caso 18.2).\n" +
            $"Data: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\nComputer: {computer}\nUtente: {user}\nCartella: {folder}\n" +
            "In un attacco vero, al posto di questo file partirebbe un comando scelto dall'attaccante.\n";
        File.WriteAllText(evidencePath, evidence);

        LastReport = new DangerousTypeReport(computer, user, folder, evidencePath);
    }
}

public sealed record DangerousTypeReport(string Computer, string User, string Folder, string EvidencePath);
