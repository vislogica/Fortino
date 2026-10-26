using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace Fortino.Web.Data;

// Crea le connessioni al database SQLite. Tutto il codice passa da qui.
public sealed class DbConnectionFactory
{
    public DbConnectionFactory(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var raw = configuration.GetConnectionString("Fortino")
            ?? throw new InvalidOperationException("Manca la stringa di connessione 'Fortino'.");
        var builder = new SqliteConnectionStringBuilder(raw);
        // Un percorso relativo si intende rispetto alla cartella del progetto.
        if (!Path.IsPathRooted(builder.DataSource))
            builder.DataSource = Path.Combine(environment.ContentRootPath, builder.DataSource);
        Directory.CreateDirectory(Path.GetDirectoryName(builder.DataSource)!);
        ConnectionString = builder.ToString();
    }

    public string ConnectionString { get; }

    public async Task<DbConnection> OpenAsync(CancellationToken ct = default)
    {
        var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
