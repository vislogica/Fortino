using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Lab;

namespace Fortino.Web.Chapters.Ch16.Scenarios;

// Base comune dei due casi del capitolo 16.
public abstract class Ch16Case : LabCaseBase
{
    public override int Chapter => 16;
    public override string Owasp => "OWASP A04:2025 Insecure Design (TOCTOU / race condition)";
    public override string Iso27001 => "8.26";
    public override string TestClass => "tests/Fortino.Tests/Ch16/Ch16RaceConditionTests.cs";

    // Legge un singolo valore dal database, per contare cosa è successo davvero.
    protected static async Task<T> ScalarAsync<T>(LabContext context, string sql, object? args = null)
    {
        await using var db = await context.Services.GetRequiredService<DbConnectionFactory>().OpenAsync();
        return await db.ExecuteScalarAsync<T>(sql, args);
    }
}
