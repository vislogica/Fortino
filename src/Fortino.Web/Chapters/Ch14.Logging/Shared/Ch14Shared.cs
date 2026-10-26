using System.Data.Common;
using Dapper;
using Fortino.Web.Data;

namespace Fortino.Web.Chapters.Ch14;

// Dati comuni ai due casi del capitolo 14.

#region libro: campo-sensibile
// Un'etichetta da mettere sui campi che non devono mai finire nel registro.
[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveAttribute : Attribute
{
}

// La richiesta di accesso: la password è segnata come sensibile.
public sealed record LoginAttemptRequest(string Username, [property: Sensitive] string Password);
#endregion

public static class Ch14Shared
{
    // Disattiva un utente (operazione sensibile): è l'azione che il caso 14.2 vuole veder tracciata.
    public static async Task DeactivateUserAsync(DbConnectionFactory factory, int userId)
    {
        // PASSO 1: si apre la connessione al database.
        await using DbConnection connection = await factory.OpenAsync();

        // PASSO 2: si segna l'utente come non attivo.
        string sql = "UPDATE AppUser SET IsActive = 0 WHERE Id = @userId;";
        object parameters = new { userId };
        await connection.ExecuteAsync(sql, parameters);
    }
}
