using System.Data.Common;

namespace Fortino.Web.Lab;

// Ogni capitolo è un modulo: registra i suoi servizi, i suoi indirizzi
// (versione vulnerabile e versione corretta) e, se servono, le sue tabelle.
// Program.cs trova da solo tutte le classi che implementano questa interfaccia.
public interface ILabChapterModule
{
    int Chapter { get; }

    void AddServices(IServiceCollection services);

    void MapEndpoints(IEndpointRouteBuilder app);

    Task InitializeDatabaseAsync(DbConnection db, DbTransaction tx, CancellationToken ct) => Task.CompletedTask;
}
