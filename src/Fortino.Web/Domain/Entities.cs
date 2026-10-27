namespace Fortino.Web.Domain;

// Il dominio di Carta & Co., distributore immaginario di forniture per ufficio.
// Sono classi semplici: Dapper le riempie leggendo le colonne con lo stesso nome.

public enum UserRole { Admin = 0, Operator = 1, Customer = 2 }

public enum OrderStatus { Draft = 0, Approved = 1, Shipped = 2, Cancelled = 3 }

public enum InvoiceStatus { Draft = 0, Issued = 1, Paid = 2 }

public sealed class AppUser
{
    public int Id { get; init; }
    public string Username { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Email { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public UserRole Role { get; init; }
    public int? CustomerId { get; init; }   // valorizzato solo per gli utenti del portale clienti
    public bool IsActive { get; init; }
}

public sealed class Customer
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string VatNumber { get; init; } = "";
    public string Email { get; init; } = "";
    public string Address { get; init; } = "";
    public string Iban { get; init; } = "";
    public decimal CreditLimit { get; init; }
}

public sealed class Product
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public decimal UnitPrice { get; init; }
}

public sealed class Order
{
    public int Id { get; init; }
    public string Number { get; init; } = "";
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public DateTime OrderDate { get; init; }
    public OrderStatus Status { get; init; }
    public string? Notes { get; init; }
    public decimal Total { get; init; }
}

public sealed class OrderLine
{
    public int Id { get; init; }
    public int OrderId { get; init; }
    public int ProductId { get; init; }
    public string Description { get; init; } = "";
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public sealed class Invoice
{
    public int Id { get; init; }
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public int? OrderId { get; init; }
    public int Year { get; init; }
    public int? Number { get; init; }        // assegnato all'emissione: la bozza non ha numero
    public DateTime? IssueDate { get; init; }
    public InvoiceStatus Status { get; init; }
    public decimal Total { get; init; }

    public string DisplayNumber => Number is null ? "—" : $"{Year}/{Number:000}";
}

public sealed class InvoiceLine
{
    public int Id { get; init; }
    public int InvoiceId { get; init; }
    public string Description { get; init; } = "";
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public sealed class Coupon
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public int DiscountPercent { get; init; }
    public bool IsUsed { get; init; }
    public int? UsedByOrderId { get; init; }
}

// Le impostazioni dell'azienda (una sola riga): l'IBAN su cui i clienti pagano le fatture.
public sealed class CompanySettings
{
    public string CompanyName { get; init; } = "";
    public string PaymentIban { get; init; } = "";
}

public sealed class StoredDocument
{
    public int Id { get; init; }
    public int? OrderId { get; init; }
    public string OriginalName { get; init; } = "";
    public string StoredName { get; init; } = "";
    public string ContentType { get; init; } = "";
    public long Size { get; init; }
    public DateTime UploadedAt { get; init; }
}

public sealed class MailMessage
{
    public int Id { get; init; }
    public string Recipient { get; init; } = "";
    public string Subject { get; init; } = "";
    public string HtmlBody { get; init; } = "";
    public DateTime SentAt { get; init; }
}
