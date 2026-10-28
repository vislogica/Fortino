namespace Fortino.Web.Chapters.Ch18.Secure;

// VERSIONE CORRETTA. Le stesse importazioni, con i metodi di lettura sicuri.
public static class ImportApi
{
    // Caso 18.1: «Importa un cliente da XML» della pagina Clienti.
    public static void MapXml(RouteGroupBuilder group)
    {
        group.MapPost("/import/xml", async (IFormFile file, Ch18Imports imports) =>
        {
            // PASSO 1: si legge il file ricevuto.
            string xml = await Ch18Imports.ReadAsync(file);

            try
            {
                // PASSO 2: lo si interpreta con le impostazioni sicure.
                string? name = SafeXmlImport.ReadCustomerName(xml);
                return await imports.AddCustomerAsync(name);
            }
            catch (System.Xml.XmlException)
            {
                // PASSO 3: un XML che prova a usare una DTD viene rifiutato dalla lettura sicura.
                return Ch18Imports.Refused("/customers", "Il file contiene una DTD: per sicurezza non viene letto.");
            }
        });
    }

    // Caso 18.2: «Importa un ordine da file» della pagina Ordini.
    public static void MapJson(RouteGroupBuilder group)
    {
        group.MapPost("/import/json", async (IFormFile file) =>
        {
            // PASSO 1: si legge il file ricevuto.
            string json = await Ch18Imports.ReadAsync(file);

            try
            {
                // PASSO 2: lo si interpreta nel tipo fisso ImportedOrder.
                SafeDeserialization.ImportedOrder? order = SafeDeserialization.Read(json);
                return Ch18Imports.ShowImported(order);
            }
            catch (System.Text.Json.JsonException)
            {
                // PASSO 3: un campo non previsto, come "$type", fa rifiutare il file.
                return Ch18Imports.Refused("/orders", "Il file contiene campi non previsti per un ordine: non viene importato.");
            }
        });
    }
}
