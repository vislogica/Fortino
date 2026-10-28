using System.Data.Common;
using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch16;

// Il contorno dei due casi del capitolo 16, uguale per le due versioni: non è codice mostrato nel libro.
public static class Ch16Shared
{
    // La finestra fra controllo e scrittura. Nella realtà sono millisecondi; nel Laboratorio 3 secondi,
    // il tempo di premere il pulsante in due schede del browser. I test la accorciano.
    public static TimeSpan CheckDelay { get; set; } = TimeSpan.FromSeconds(3);

    // Una connessione che, se il database è occupato da un'altra richiesta, aspetta invece di fallire.
    public static async Task<DbConnection> OpenAsync(DbConnectionFactory factory)
    {
        DbConnection connection = await factory.OpenAsync();
        await connection.ExecuteAsync("PRAGMA busy_timeout = 5000;");
        return connection;
    }

    // Lo sconto del buono, come riga dell'ordine con importo negativo: si vede nel totale.
    public static async Task AddDiscountLineAsync(DbConnection connection, string code, int orderId)
    {
        await connection.ExecuteAsync(
            """
            INSERT INTO OrderLine (OrderId, ProductId, Description, Quantity, UnitPrice)
            SELECT @orderId,
                   (SELECT ProductId FROM OrderLine WHERE OrderId = @orderId ORDER BY Id LIMIT 1),
                   'Sconto buono ' || c.Code || ' (' || c.DiscountPercent || '%)', 1,
                   -ROUND((SELECT SUM(Quantity * UnitPrice) FROM OrderLine WHERE OrderId = @orderId AND UnitPrice > 0)
                          * c.DiscountPercent / 100.0, 2)
              FROM Coupon c WHERE c.Code = @code;
            """, new { orderId, code });
    }
}

// Il modulo «Applica il buono» del portale.
public sealed class CouponForm
{
    public string? Code { get; set; }
}
