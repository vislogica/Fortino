using System.Xml;

namespace Fortino.Web.Chapters.Ch18.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Legge un XML con impostazioni permissive: le definizioni di tipo documento (DTD) sono
// ammesse e le risorse esterne vengono risolte. Così un XML può far aprire al server
// un file o un indirizzo indicato dall'XML stesso.
public static class UnsafeXmlImport
{
    #region libro: xml-vulnerabile
    // Impostazioni permissive: la DTD viene elaborata e un resolver apre le risorse esterne.
    // ESEMPIO di attacco: un XML con <!ENTITY segreto SYSTEM "file:///.../appsettings.json"> e
    // <nome>&segreto;</nome> fa aprire al server quel file e lo rimanda dentro il nome del cliente.
    private static readonly XmlReaderSettings UnsafeSettings = new()
    {
        DtdProcessing = DtdProcessing.Parse,
        XmlResolver = new XmlUrlResolver(),
    };

    public static string? ReadCustomerName(string xml)
    {
        // PASSO 1: si prepara la lettura del testo XML, con le impostazioni permissive.
        using StringReader text = new StringReader(xml);
        using XmlReader reader = XmlReader.Create(text, UnsafeSettings);

        // PASSO 2: si carica il documento. La DTD viene elaborata: se l'XML dichiara un'entità
        // esterna (per esempio un file del server), il suo contenuto entra nel documento.
        XmlDocument document = new XmlDocument();
        document.XmlResolver = new XmlUrlResolver();
        document.Load(reader);

        // PASSO 3: si legge il nome del cliente, che ora può contenere il file del server.
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
