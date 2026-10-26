using Fortino.Web.Domain;
using MudBlazor;

namespace Fortino.Web.Components;

// Testi e colori in italiano per ruoli e stati.
public static class Labels
{
    public static string Role(UserRole role) => role switch
    {
        UserRole.Admin => "Amministratore",
        UserRole.Operator => "Operatore",
        _ => "Cliente",
    };

    public static string Role(string? role) =>
        Enum.TryParse<UserRole>(role, out var parsed) ? Role(parsed) : "";

    public static (string Text, Color Color) Order(OrderStatus status) => status switch
    {
        OrderStatus.Draft => ("Bozza", Color.Default),
        OrderStatus.Approved => ("Approvato", Color.Info),
        OrderStatus.Shipped => ("Spedito", Color.Success),
        _ => ("Annullato", Color.Error),
    };

    public static (string Text, Color Color) Invoice(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => ("Bozza", Color.Default),
        InvoiceStatus.Issued => ("Emessa", Color.Info),
        _ => ("Incassata", Color.Success),
    };

    public static string Money(decimal amount) => amount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("it-IT"));
}
