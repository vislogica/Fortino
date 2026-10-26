-- Schema del database di Fortino (SQLite).
-- Gli importi sono NUMERIC: in SQL Server si userebbe DECIMAL(18,2).

CREATE TABLE Customer (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT    NOT NULL,
    VatNumber   TEXT    NOT NULL,
    Email       TEXT    NOT NULL,
    Address     TEXT    NOT NULL,
    Iban        TEXT    NOT NULL,
    CreditLimit NUMERIC NOT NULL DEFAULT 0
);

CREATE TABLE AppUser (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Username     TEXT    NOT NULL UNIQUE COLLATE NOCASE,
    DisplayName  TEXT    NOT NULL,
    Email        TEXT    NOT NULL,
    PasswordHash TEXT    NOT NULL,
    Role         INTEGER NOT NULL,
    CustomerId   INTEGER NULL REFERENCES Customer(Id),
    IsActive     INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE Product (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    Code      TEXT    NOT NULL UNIQUE,
    Name      TEXT    NOT NULL,
    UnitPrice NUMERIC NOT NULL
);

CREATE TABLE SalesOrder (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    Number     TEXT    NOT NULL UNIQUE,
    CustomerId INTEGER NOT NULL REFERENCES Customer(Id),
    OrderDate  TEXT    NOT NULL,
    Status     INTEGER NOT NULL,
    Notes      TEXT    NULL
);

CREATE TABLE OrderLine (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId     INTEGER NOT NULL REFERENCES SalesOrder(Id),
    ProductId   INTEGER NOT NULL REFERENCES Product(Id),
    Description TEXT    NOT NULL,
    Quantity    INTEGER NOT NULL,
    UnitPrice   NUMERIC NOT NULL
);

CREATE TABLE Invoice (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL REFERENCES Customer(Id),
    OrderId    INTEGER NULL REFERENCES SalesOrder(Id),
    Year       INTEGER NOT NULL,
    Number     INTEGER NULL,
    IssueDate  TEXT    NULL,
    Status     INTEGER NOT NULL,
    Total      NUMERIC NOT NULL,
    -- Dati copiati al momento dell'emissione: il documento emesso non cambia se cambiano
    -- l'anagrafica del cliente o le coordinate bancarie dell'azienda (capitolo 7).
    IssuedCustomerName    TEXT NULL,
    IssuedCustomerAddress TEXT NULL,
    IssuedPaymentIban     TEXT NULL
);
-- Un numero di fattura non si ripete nello stesso anno.
CREATE UNIQUE INDEX UX_Invoice_Year_Number ON Invoice(Year, Number) WHERE Number IS NOT NULL;

CREATE TABLE InvoiceLine (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    InvoiceId   INTEGER NOT NULL REFERENCES Invoice(Id),
    Description TEXT    NOT NULL,
    Quantity    INTEGER NOT NULL,
    UnitPrice   NUMERIC NOT NULL
);

-- Impostazioni dell'azienda: una sola riga.
CREATE TABLE CompanySettings (
    Id          INTEGER PRIMARY KEY CHECK (Id = 1),
    CompanyName TEXT NOT NULL,
    PaymentIban TEXT NOT NULL      -- IBAN su cui i clienti pagano le fatture
);

CREATE TABLE Coupon (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Code            TEXT    NOT NULL UNIQUE,
    DiscountPercent INTEGER NOT NULL,
    IsUsed          INTEGER NOT NULL DEFAULT 0,
    UsedByOrderId   INTEGER NULL REFERENCES SalesOrder(Id)
);

CREATE TABLE StoredDocument (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId      INTEGER NULL REFERENCES SalesOrder(Id),
    OriginalName TEXT    NOT NULL,
    StoredName   TEXT    NOT NULL,
    ContentType  TEXT    NOT NULL,
    Size         INTEGER NOT NULL,
    UploadedAt   TEXT    NOT NULL
);

CREATE TABLE MailMessage (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    Recipient TEXT NOT NULL,
    Subject   TEXT NOT NULL,
    HtmlBody  TEXT NOT NULL,
    SentAt    TEXT NOT NULL
);
