namespace Fortino.Web.Chapters.Ch05.Vulnerable;

// Segnaposto. Il capitolo 5 non ha un attacco eseguibile nel Laboratorio: il rischio — usare un
// pacchetto con una vulnerabilità nota — si mostra con un comando, non con due versioni del codice.
// L'interruttore "Vulnerabile" resta disabilitato; il testo del libro spiega il caso.
public static class NotIncludedYet
{
    #region libro: componenti-vulnerabili-da-integrare
    // La "versione vulnerabile" di questo capitolo è semplicemente un pacchetto non aggiornato con
    // una vulnerabilità nota. Si scopre con: dotnet list package --vulnerable --include-transitive.
    #endregion
}
