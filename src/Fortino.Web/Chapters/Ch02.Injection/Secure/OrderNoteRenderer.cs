using System.Text.Encodings.Web;
using Fortino.Web.Data;
using Microsoft.AspNetCore.Components;

namespace Fortino.Web.Chapters.Ch02.Secure;

// VERSIONE CORRETTA.
// Prepara la nota mostrata dalla pagina dello staff /orders/{numero}.
public sealed class OrderNoteRenderer(DbConnectionFactory factory)
{
    #region libro: xss-corretto
    // Caso 2.2. La nota viene codificata prima di entrare nell'HTML: i caratteri speciali come
    // < e > diventano innocui, e un eventuale comando resta testo visibile, non codice eseguito.
    public async Task<MarkupString> RenderAsync(int orderId)
    {
        // PASSO 1: si legge dal database la nota scritta dal cliente.
        string noteText = await Ch02Shared.OrderNoteAsync(factory, orderId);

        // PASSO 2: si codifica la nota. <img diventa &lt;img: il browser lo mostra
        // come testo e non lo esegue.
        string safeNoteText = HtmlEncoder.Default.Encode(noteText);

        // PASSO 3: si costruisce l'HTML con la nota già codificata.
        string html = $"<div class=\"nota\">{safeNoteText}</div>";

        // PASSO 4: ora l'HTML contiene solo testo innocuo, e si può consegnare a Blazor così com'è.
        MarkupString markup = new MarkupString(html);
        return markup;
    }
    #endregion
}
