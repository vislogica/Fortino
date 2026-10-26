using System.Security.Claims;
using Fortino.Web.Auth;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch13.Secure;

// VERSIONE CORRETTA.
public static class ErrorHandlingApi
{
    public static void Map(RouteGroupBuilder group, ILoggerFactory loggerFactory)
    {
        ILogger logger = loggerFactory.CreateLogger("Ch13.ErrorHandling");

        #region libro: errore-dettagli-corretto
        // Caso 13.1. Il dettaglio dell'errore si scrive nel registro interno, con un riferimento;
        // al client va solo un messaggio generico e lo stesso riferimento, per l'assistenza.
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
                // PASSO 2: un riferimento breve e casuale, per ritrovare l'errore nel registro.
                string guidText = Guid.NewGuid().ToString("N");
                string reference = guidText.Substring(0, 8);

                // PASSO 3: il dettaglio tecnico va solo nel registro interno.
                logger.LogWarning(ex, "Emissione fattura non riuscita (rif. {Reference}).", reference);

                // PASSO 4: al client va un messaggio generico, con il riferimento per l'assistenza.
                string message = $"Operazione non riuscita. Riferimento per l'assistenza: {reference}.";
                IResult response = Results.Problem(detail: message, statusCode: StatusCodes.Status500InternalServerError);
                return response;
            }
        });
        #endregion

        #region libro: fail-open-corretto
        // Caso 13.2. Il risultato del controllo parte da "non autorizzato"; se il controllo
        // fallisce, resta "non autorizzato" (fail closed): nel dubbio si nega, non si concede.
        group.MapGet("/orders/{orderId:int}/note", async (int orderId, ClaimsPrincipal user, DbConnectionFactory factory) =>
        {
            // PASSO 1: si parte da "non autorizzato".
            bool isOwner = false;
            try
            {
                // PASSO 2: si legge il cliente dell'utente con TryParse: se manca, non c'è
                // eccezione e isOwner resta false.
                string? customerIdText = user.FindFirstValue(FortinoClaims.CustomerId);
                bool hasCustomerId = int.TryParse(customerIdText, out int customerId);
                if (hasCustomerId)
                {
                    // PASSO 3: si controlla che l'ordine sia del cliente.
                    isOwner = await Ch13Shared.IsOwnerAsync(factory, orderId, customerId);
                }
            }
            catch (Exception ex)
            {
                // PASSO 4: il controllo è fallito: si registra l'errore e si resta su
                // "non autorizzato" (fail closed).
                logger.LogWarning(ex, "Controllo del proprietario non riuscito per l'ordine {OrderId}.", orderId);
                isOwner = false;
            }

            // PASSO 5: la nota si mostra solo a chi è sicuramente il proprietario.
            if (!isOwner)
            {
                return Results.Forbid();
            }
            IResult response = Results.Ok(new { note = Ch13Shared.OrderNote });
            return response;
        });
        #endregion
    }
}
