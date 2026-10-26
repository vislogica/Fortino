using System.Data.Common;
using Dapper;
using Fortino.Web.Infrastructure;
using Fortino.Web.Lab;

namespace Fortino.Web.Data;

// Crea il database al primo avvio e lo riporta allo stato iniziale con "Ripristina i dati".
public sealed class DatabaseInitializer(
    DbConnectionFactory factory,
    IEnumerable<ILabChapterModule> chapters,
    InMemoryLogStore logStore,
    ILogger<DatabaseInitializer> logger)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task EnsureCreatedAsync(CancellationToken ct = default)
    {
        await Gate.WaitAsync(ct);
        try
        {
            await using var db = await factory.OpenAsync(ct);
            var exists = await db.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Customer';");
            if (exists == 0)
                await CreateAndSeedAsync(db, ct);
        }
        finally { Gate.Release(); }
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        await Gate.WaitAsync(ct);
        try
        {
            await using var db = await factory.OpenAsync(ct);
            await db.ExecuteAsync("PRAGMA foreign_keys = OFF;");
            var tables = await db.QueryAsync<string>(
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';");
            foreach (var table in tables)
                await db.ExecuteAsync($"DROP TABLE \"{table}\";"); // nomi letti dal catalogo, non dall'utente
            await db.ExecuteAsync("PRAGMA foreign_keys = ON;");
            await CreateAndSeedAsync(db, ct);
            logStore.Clear();
            logger.LogInformation("Dati ripristinati allo stato iniziale.");
        }
        finally { Gate.Release(); }
    }

    private async Task CreateAndSeedAsync(DbConnection db, CancellationToken ct)
    {
        await db.ExecuteAsync("PRAGMA journal_mode = WAL;");
        await using var tx = await db.BeginTransactionAsync(ct);
        await db.ExecuteAsync(ReadSchema(), transaction: tx);
        await SeedData.InsertAsync(db, tx);
        // Ogni capitolo può aggiungere le sue tabelle e i suoi dati.
        foreach (var chapter in chapters)
            await chapter.InitializeDatabaseAsync(db, tx, ct);
        await tx.CommitAsync(ct);
    }

    private static string ReadSchema()
    {
        using var stream = typeof(DatabaseInitializer).Assembly.GetManifestResourceStream("Fortino.Schema.sql")
            ?? throw new InvalidOperationException("Schema del database non trovato.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
