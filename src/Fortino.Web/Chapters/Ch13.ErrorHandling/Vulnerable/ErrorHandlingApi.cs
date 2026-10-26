using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch13.Vulnerable;

// VERSIONE VULNERABILE. Codice volutamente insicuro, solo per il Laboratorio.
public static class ErrorHandlingApi
{
    public static void Map(RouteGroupBuilder group)
    {
        #region libro: errore-dettagli-vulnerabile
        // Caso 13.1. Se l'operazione fallisce, il messaggio tecnico dell'eccezione (con il testo
        // dell'errore del database, cioè nomi di tabelle e colonne) viene restituito al client.
        group.MapPost("/invoices/issue", async (IssueInvoiceRequest request, DbConnectionFactory factory) =>
        {
            try
            {
                // PASSO 1: si prova a emettere la fattura.
                await Ch13Shared.IssueInvoiceAsync(factory, request);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                // PASSO 2: qualcosa è andato storto. Il testo completo dell'eccezione, con nomi di
                // tabelle, colonne e righe di codice, finisce nella risposta al client.
                string errorDetails = ex.ToString();
                IResult response = Results.Problem(detail: errorDetails, statusCode: StatusCodes.Status500InternalServerError);
                return response;
            }
        });
        #endregion

        #region libro: fail-open-vulnerabile
        // Caso 13.2. Solo il cliente proprietario dovrebbe leggere la nota dell'ordine. Ma se il
        // controllo del proprietario lancia un'eccezione, il codice la ignora e lascia passare.
        group.MapGet("/orders/{orderId:int}/note", async (int orderId, ClaimsPrincipal user, DbConnectionFactory factory) =>
        {
            try
            {
                // PASSO 1: si legge il cliente dell'utente. Se l'utente non è un cliente (per esempio
                // è un operatore) il valore manca e int.Parse lancia un'eccezione.
                string? customerIdText = user.FindFirstValue(FortinoClaims.CustomerId);
                int customerId = int.Parse(customerIdText!);

                // PASSO 2: si controlla che l'ordine sia del cliente.
                bool isOwner = await Ch13Shared.IsOwnerAsync(factory, orderId, customerId);
                if (!isOwner)
                {
                    return Results.Forbid();
                }
            }
            catch
            {
                // PASSO 3: il controllo è fallito: si tira dritto invece di negare (fail open).
            }

            // PASSO 4: si restituisce la nota riservata a chiunque sia arrivato fin qui.
            IResult response = Results.Ok(new { note = Ch13Shared.OrderNote });
            return response;
        });
        #endregion
    }
}
