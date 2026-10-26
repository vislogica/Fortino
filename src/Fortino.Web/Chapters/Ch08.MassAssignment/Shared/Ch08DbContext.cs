using Fortino.Web.Chapters.Ch08.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fortino.Web.Chapters.Ch08;

// Il capitolo 8 usa EF Core invece di Dapper: il mass assignment nasce soprattutto quando
// un'entità del database viene riempita direttamente con i dati della richiesta.
// Le tabelle sono le stesse del resto dell'applicazione.
public sealed class Ch08DbContext(DbContextOptions<Ch08DbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>().ToTable("AppUser");
        model.Entity<Order>().ToTable("SalesOrder");
        model.Entity<OrderLine>().ToTable("OrderLine");
        model.Entity<Product>().ToTable("Product");
    }
}
