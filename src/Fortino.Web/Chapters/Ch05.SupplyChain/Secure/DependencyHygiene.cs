namespace Fortino.Web.Chapters.Ch05.Secure;

// VERSIONE CORRETTA (processo). Il capitolo 5 non è un attacco da eseguire, ma un'abitudine:
// controllare i pacchetti usati e tenerli aggiornati. Qui sono raccolti i comandi e le impostazioni.
public static class DependencyHygiene
{
    #region libro: controllo-dipendenze
    // Elenca i pacchetti con vulnerabilità note, diretti e trascinati da altri (transitivi).
    // Da eseguire a ogni build e nella pipeline di integrazione continua (CI):
    public const string CheckVulnerable = "dotnet list package --vulnerable --include-transitive";

    // Elenca i pacchetti per cui esiste una versione più recente:
    public const string CheckOutdated = "dotnet list package --outdated";

    // Nel file di progetto (.csproj), per fissare le versioni e accorgersi di ogni cambiamento:
    //   <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
    // e, nella CI, il ripristino in "modalità bloccata", che fallisce se il lock non combacia:
    //   dotnet restore --locked-mode
    public const string LockFileSetting = "<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>";
    #endregion
}
