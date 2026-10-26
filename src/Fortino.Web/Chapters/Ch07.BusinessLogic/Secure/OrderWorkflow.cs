using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch07.Secure;

#region libro: macchina-a-stati
// Le regole del ciclo di vita di un ordine, in un solo posto:
// da ogni stato si può andare solo negli stati elencati.
public static class OrderWorkflow
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [OrderStatus.Draft]     = [OrderStatus.Approved, OrderStatus.Cancelled],
        [OrderStatus.Approved]  = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped]   = [],
        [OrderStatus.Cancelled] = [],
    };

    public static bool CanMove(OrderStatus from, OrderStatus to)
    {
        // PASSO 1: si cercano gli stati raggiungibili da quello di partenza.
        bool fromIsKnown = Allowed.TryGetValue(from, out OrderStatus[]? reachableStatuses);
        if (!fromIsKnown || reachableStatuses is null)
        {
            return false;
        }

        // PASSO 2: il passaggio è ammesso solo se lo stato di arrivo è nell'elenco.
        bool isAllowed = reachableStatuses.Contains(to);
        return isAllowed;
    }
}
#endregion
