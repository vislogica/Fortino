using Fortino.Web.Data;
using Microsoft.AspNetCore.Components;

namespace Fortino.Web.Chapters.Ch02.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
// Prepara la nota mostrata dalla pagina dello staff /orders/{numero}.
public sealed class OrderNoteRenderer(DbConnectionFactory factory)
{
    #region libro: xss-vulnerabile
    // Caso 2.2. La nota l'ha scritta il cliente nel portale, ed è stata salvata nel database così
    // com'è. Qui la si rilegge e la si mette nell'HTML senza codificarla: se contiene un comando
    // per il browser, il browser di chi apre la pagina lo esegue.
    public async Task<MarkupString> RenderAsync(int orderId)
    {
        // PASSO 1: si legge dal database la nota scritta dal cliente.
        string noteText = await Ch02Shared.OrderNoteAsync(factory, orderId);

        // PASSO 2: si costruisce l'HTML concatenando la nota così com'è.
        // Se noteText vale  <img src=x onerror="alert('XSS')">  il comando diventa parte della pagina.
        string html = $"<div class=\"nota\">{noteText}</div>";

        // PASSO 3: MarkupString dice a Blazor "questo è HTML già pronto, non codificarlo".
        // Il browser lo interpreta, ed esegue il comando nascosto nella nota.
        MarkupString markup = new MarkupString(html);
        return markup;
    }
    #endregion
}
