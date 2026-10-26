using System.Security.Claims;
using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Domain;

namespace Fortino.Web.Auth;

public static class FortinoClaims
{
    public const string DisplayName = "fortino:display_name";
    public const string CustomerId = "fortino:customer_id";

    public static ClaimsPrincipal CreatePrincipal(AppUser user, string authenticationType)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(DisplayName, user.DisplayName),
        };
        if (user.CustomerId is int customerId)
            claims.Add(new Claim(CustomerId, customerId.ToString()));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
    }

    public static int? GetCustomerId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(CustomerId), out var id) ? id : null;

    public static int? GetUserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static string GetDisplayName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(DisplayName) ?? principal.Identity?.Name ?? "";
}

public static class Policies
{
    public const string Staff = "Staff";        // amministratori e operatori di Carta & Co.
    public const string Customer = "Customer";  // utenti del portale clienti
}

public sealed class UserStore(DbConnectionFactory factory)
{
    public async Task<AppUser?> FindByUsernameAsync(string username)
    {
        await using var db = await factory.OpenAsync();
        return await db.QuerySingleOrDefaultAsync<AppUser>(
            "SELECT * FROM AppUser WHERE Username = @username;", new { username });
    }

    public async Task<AppUser?> FindByIdAsync(int id)
    {
        await using var db = await factory.OpenAsync();
        return await db.QuerySingleOrDefaultAsync<AppUser>("SELECT * FROM AppUser WHERE Id = @id;", new { id });
    }

    public async Task<IReadOnlyList<AppUser>> GetActiveAsync()
    {
        await using var db = await factory.OpenAsync();
        return (await db.QueryAsync<AppUser>("SELECT * FROM AppUser WHERE IsActive = 1 ORDER BY Role, DisplayName;")).AsList();
    }

    // Tutti gli utenti, attivi e disattivati: li vede l'amministratore nella pagina /users.
    public async Task<IReadOnlyList<AppUser>> GetAllAsync()
    {
        await using var db = await factory.OpenAsync();
        return (await db.QueryAsync<AppUser>("SELECT * FROM AppUser ORDER BY Role, DisplayName;")).AsList();
    }

    public async Task SetActiveAsync(int id, bool isActive)
    {
        await using var db = await factory.OpenAsync();
        await db.ExecuteAsync("UPDATE AppUser SET IsActive = @isActive WHERE Id = @id;", new { id, isActive });
    }
}
