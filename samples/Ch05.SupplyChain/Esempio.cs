using System.Text.Encodings.Web;

namespace Ch05.SupplyChain;

// Usa il pacchetto vecchio, così non è una dipendenza "inutilizzata": la vulnerabilità nota
// riguarda proprio questo componente. Il codice in sé è banale; conta la versione del pacchetto.
public static class Esempio
{
    public static string CodificaHtml(string testo) => HtmlEncoder.Default.Encode(testo);
}
