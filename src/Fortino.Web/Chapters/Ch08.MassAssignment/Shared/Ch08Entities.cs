using Fortino.Web.Domain;

namespace Fortino.Web.Chapters.Ch08.Entities;

// Le entità di EF Core del capitolo 8: una classe per tabella, con proprietà che si
// leggono e si scrivono. Sono le classi che EF Core salva nel database.

#region libro: entita-utente
public sealed class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; }          // Admin, Operator o Customer
    public int? CustomerId { get; set; }        // il cliente a cui appartiene l'utente del portale
    public bool IsActive { get; set; }
}
#endregion

#region libro: entita-riga-ordine
public sealed class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string Description { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
#endregion

// Di ordini e prodotti servono solo poche colonne.
public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public OrderStatus Status { get; set; }
}

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal UnitPrice { get; set; }
}
