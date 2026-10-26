using System.Data.Common;
using Dapper;
using Fortino.Web.Domain;
using Microsoft.AspNetCore.Identity;

namespace Fortino.Web.Data;

// I dati iniziali di Carta & Co. Sono sempre gli stessi, così gli esempi del libro
// trovano gli stessi clienti, ordini e numeri di fattura.
public static class SeedData
{
    // Password di tutti gli utenti dimostrativi. È scritta nel README: serve solo in locale.
    public const string DemoPassword = "Fortino-Demo-2026";

    public static readonly (string Username, string DisplayName, string Email, UserRole Role, int? CustomerId)[] Users =
    [
        ("anna",   "Anna Rossi",   "anna.rossi@cartaeco.example",   UserRole.Admin,    null),
        ("mario",  "Mario Bianchi", "mario.bianchi@cartaeco.example", UserRole.Operator, null),
        ("verdi",  "Studio Verdi", "amministrazione@studioverdi.example", UserRole.Customer, 1),
        ("lumen",  "Ottica Lumen", "ordini@otticalumen.example",   UserRole.Customer, 2),
    ];

    public static async Task InsertAsync(DbConnection db, DbTransaction tx)
    {
        await db.ExecuteAsync(
            """
            INSERT INTO Customer (Id, Name, VatNumber, Email, Address, Iban, CreditLimit) VALUES
              (1, 'Studio Verdi',    'IT01234567890', 'amministrazione@studioverdi.example', 'Via dei Tigli 12, Verona',      'IT60X0542811101000000123456', 5000),
              (2, 'Ottica Lumen',    'IT09876543210', 'ordini@otticalumen.example',          'Corso Nuovo 8, Vicenza',        'IT02L1234512345123456789012', 3000),
              (3, 'Panificio Sole',  'IT05566778899', 'info@panificiosole.example',          'Piazza Grande 3, Padova',       'IT45R0300203280000400162854', 1500),
              (4, 'Ferramenta Nord', 'IT04433221100', 'acquisti@ferramentanord.example',     'Via Industria 41, Trento',      'IT88Q0200801600000105678901', 8000);

            INSERT INTO Product (Id, Code, Name, UnitPrice) VALUES
              (1, 'CA-A4-500', 'Carta A4 80 g, risma da 500 fogli', 5.90),
              (2, 'TO-BK-12',  'Toner nero per stampante laser',     64.00),
              (3, 'PE-BL-50',  'Penne a sfera blu, confezione da 50', 18.50),
              (4, 'RA-DOR-10', 'Raccoglitori a due anelli, 10 pezzi', 32.00),
              (5, 'SE-ERG-01', 'Sedia ergonomica da ufficio',        189.00),
              (6, 'BU-C5-500', 'Buste C5 con finestra, 500 pezzi',   27.40);

            INSERT INTO SalesOrder (Id, Number, CustomerId, OrderDate, Status, Notes) VALUES
              (1, 'ORD-2026-0101', 1, '2026-09-28', 2, 'Consegna al secondo piano'),
              (2, 'ORD-2026-0102', 2, '2026-10-02', 2, NULL),
              (3, 'ORD-2026-0103', 1, '2026-10-09', 1, NULL),
              (4, 'ORD-2026-0104', 3, '2026-10-12', 1, 'Chiamare prima della consegna'),
              (5, 'ORD-2026-0105', 2, '2026-10-15', 0, NULL),
              (6, 'ORD-2026-0106', 4, '2026-10-16', 0, NULL);

            INSERT INTO OrderLine (OrderId, ProductId, Description, Quantity, UnitPrice) VALUES
              (1, 1, 'Carta A4 80 g, risma da 500 fogli', 40, 5.90),
              (1, 2, 'Toner nero per stampante laser',     5, 64.00),
              (2, 3, 'Penne a sfera blu, confezione da 50', 2, 18.50),
              (2, 6, 'Buste C5 con finestra, 500 pezzi',   2, 27.40),
              (3, 5, 'Sedia ergonomica da ufficio',         4, 189.00),
              (4, 1, 'Carta A4 80 g, risma da 500 fogli',  10, 5.90),
              (4, 4, 'Raccoglitori a due anelli, 10 pezzi', 1, 32.00),
              (5, 2, 'Toner nero per stampante laser',      2, 64.00),
              (6, 5, 'Sedia ergonomica da ufficio',         6, 189.00);

            INSERT INTO Invoice (Id, CustomerId, OrderId, Year, Number, IssueDate, Status, Total) VALUES
              (1, 1, 1, 2026, 14, '2026-10-10', 2, 556.00),
              (2, 2, 2, 2026, 15, '2026-10-14', 1, 91.80),
              (3, 3, 4, 2026, NULL, NULL,       0, 91.00);

            INSERT INTO CompanySettings (Id, CompanyName, PaymentIban) VALUES
              (1, 'Carta & Co. S.r.l.', 'IT40S0542811101000000123456');

            UPDATE Invoice
               SET IssuedCustomerName    = (SELECT Name FROM Customer c WHERE c.Id = Invoice.CustomerId),
                   IssuedCustomerAddress = (SELECT Address FROM Customer c WHERE c.Id = Invoice.CustomerId),
                   IssuedPaymentIban     = (SELECT PaymentIban FROM CompanySettings)
             WHERE Status <> 0;

            INSERT INTO InvoiceLine (InvoiceId, Description, Quantity, UnitPrice) VALUES
              (1, 'Carta A4 80 g, risma da 500 fogli', 40, 5.90),
              (1, 'Toner nero per stampante laser',     5, 64.00),
              (2, 'Penne a sfera blu, confezione da 50', 2, 18.50),
              (2, 'Buste C5 con finestra, 500 pezzi',   2, 27.40),
              (3, 'Carta A4 80 g, risma da 500 fogli', 10, 5.90),
              (3, 'Raccoglitori a due anelli, 10 pezzi', 1, 32.00);

            INSERT INTO Coupon (Code, DiscountPercent, IsUsed, UsedByOrderId) VALUES
              ('BENVENUTO10', 10, 0, NULL),
              ('AUTUNNO15',   15, 0, NULL),
              ('FEDELTA20',   20, 1, 2);
            """, transaction: tx);

        var hasher = new PasswordHasher<AppUser>();
        foreach (var u in Users)
        {
            var user = new AppUser { Username = u.Username };
            await db.ExecuteAsync(
                """
                INSERT INTO AppUser (Username, DisplayName, Email, PasswordHash, Role, CustomerId, IsActive)
                VALUES (@Username, @DisplayName, @Email, @PasswordHash, @Role, @CustomerId, 1);
                """,
                new
                {
                    u.Username, u.DisplayName, u.Email,
                    PasswordHash = hasher.HashPassword(user, DemoPassword),
                    Role = (int)u.Role, u.CustomerId,
                },
                tx);
        }
    }
}
