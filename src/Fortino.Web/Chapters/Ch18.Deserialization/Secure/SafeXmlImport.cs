using System.Xml;

namespace Fortino.Web.Chapters.Ch18.Secure;

// VERSIONE CORRETTA (caso 18.1).
// Legge un XML in modo sicuro: le definizioni di tipo documento (DTD) sono vietate e nessuna
// risorsa esterna viene risolta. Così un XML non può far leggere al server file o indirizzi.
public static class SafeXmlImport
{
    #region libro: xml-corretto
    // Impostazioni sicure: niente DTD, nessun resolver per le risorse esterne.
    private static readonly XmlReaderSettings SafeSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,   // le entità esterne si definiscono nella DTD: vietandola, spariscono
        XmlResolver = null,                        // nessuna risorsa esterna viene mai aperta
        MaxCharactersFromEntities = 1024,
    };

    // Legge il valore dell'elemento <nome> dall'XML ricevuto.
    public static string? ReadCustomerName(string xml)
    {
        // PASSO 1: si prepara la lettura del testo XML, con le impostazioni sicure.
        using StringReader text = new StringReader(xml);
        using XmlReader reader = XmlReader.Create(text, SafeSettings);

        // PASSO 2: si carica il documento. Se l'XML contiene una DTD, qui la lettura si ferma
        // con un'eccezione: nessun file del server viene aperto.
        XmlDocument document = new XmlDocument();
        document.XmlResolver = null;
        document.Load(reader);

        // PASSO 3: si legge il nome del cliente.
        XmlNode? nameNode = document.SelectSingleNode("/cliente/nome");
        if (nameNode is null)
        {
            return null;
        }
        string name = nameNode.InnerText;
        return name;
    }
    #endregion
}
