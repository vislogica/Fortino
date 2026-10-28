using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Domain;
using Fortino.Web.Infrastructure;

namespace Fortino.Web.Chapters.Ch12;

// Il contorno dei due casi del capitolo 12, uguale per le due versioni: non è codice mostrato nel
// libro. Le versioni cambiano solo come calcolano l'hash della password o il codice di reset.

// Il modulo «Aggiungi un utente» della pagina /users (caso 12.1).
public sealed class NewUserForm
{
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string? Password { get; set; }
}

// Il modulo «Invia il codice di reset»: gli utenti spuntati nella tabella (caso 12.2).
public sealed class ResetForm
{
    public int[] UserIds { get; set; } = [];
}

public sealed class Ch12Users(DbConnectionFactory factory, IEmailSender mail)
{
    // Aggiunge un operatore con l'hash già calcolato, poi torna alla pagina degli utenti.
    public async Task<IResult> CreateAsync(NewUserForm form, string passwordHash)
    {
        string username = (form.Username ?? "").Trim();
        if (username.Length == 0 || string.IsNullOrEmpty(form.Password))
            return Results.LocalRedirect("/users?errore=" + Uri.EscapeDataString("Nome utente e password sono obbligatori."));

        await using var db = await factory.OpenAsync();
        bool exists = await db.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM AppUser WHERE Username = @username;", new { username }) > 0;
        if (exists)
            return Results.LocalRedirect("/users?errore=" + Uri.EscapeDataString($"Il nome utente {username} esiste già."));

        await db.ExecuteAsync(
            """
            INSERT INTO AppUser (Username, DisplayName, Email, PasswordHash, Role, CustomerId, IsActive)
            VALUES (@username, @displayName, @email, @passwordHash, @role, NULL, 1);
            """,
            new
            {
                username,
                displayName = string.IsNullOrWhiteSpace(form.DisplayName) ? username : form.DisplayName.Trim(),
                email = $"{username}@cartaeco.example",
                passwordHash,
                role = (int)UserRole.Operator,
            });
        return Results.LocalRedirect("/users?esito=salvato");
    }

    // Manda a ogni utente selezionato un'email con il suo codice di reset, nella posta finta.
    public async Task<IResult> SendResetCodesAsync(int[] userIds, Func<int, string> codeFor)
    {
        if (userIds.Length == 0)
            return Results.LocalRedirect("/users?errore=" + Uri.EscapeDataString("Seleziona almeno un utente."));

        await using var db = await factory.OpenAsync();
        var users = await db.QueryAsync<(int Id, string DisplayName, string Email)>(
            "SELECT Id, DisplayName, Email FROM AppUser WHERE Id IN @userIds;", new { userIds });
        int sent = 0;
        foreach (var user in users)
        {
            string code = codeFor(user.Id);
            await mail.SendAsync(user.Email, "Il tuo codice per reimpostare la password",
                $"Ciao {user.DisplayName},\n\nil tuo codice per reimpostare la password è: {code}\n\n" +
                "Se non l'hai chiesto tu, ignora questo messaggio.");
            sent++;
        }
        return Results.LocalRedirect($"/users?inviati={sent}");
    }
}
