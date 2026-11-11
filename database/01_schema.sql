/*
    SalesPulse ilişkisel şema.
    Hacim hedefleri: Cities 81, Categories 30, Products 5.000,
    Customers 100.000, Orders 2.000.000, OrderDetails 3.500.000, Payments 2.000.000.
    Nonclustered indeksler bu dosyada yoktur; ölçümden sonra 03_indexes.sql ekler.
*/

IF DB_ID(N'SalesPulseDb') IS NULL
BEGIN
    CREATE DATABASE SalesPulseDb COLLATE Turkish_CI_AS;
END
GO

ALTER DATABASE SalesPulseDb SET RECOVERY SIMPLE;
GO

IF OBJECT_ID(N'dbo.Payments', N'U') IS NOT NULL DROP TABLE dbo.Payments;
IF OBJECT_ID(N'dbo.OrderDetails', N'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DROP TABLE dbo.Customers;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Cities', N'U') IS NOT NULL DROP TABLE dbo.Cities;
GO

CREATE TABLE dbo.Cities
(
    CityId     int            NOT NULL CONSTRAINT PK_Cities PRIMARY KEY,
    Name       nvarchar(50)   NOT NULL,
    Region     nvarchar(40)   NOT NULL
);
GO

CREATE TABLE dbo.Categories
(
    CategoryId int            NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Name       nvarchar(80)   NOT NULL
);
GO

CREATE TABLE dbo.Products
(
    ProductId  int            NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    CategoryId int            NOT NULL,
    Name       nvarchar(160)  NOT NULL,
    UnitPrice  decimal(18, 2) NOT NULL,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (CategoryId),
    CONSTRAINT CK_Products_UnitPrice CHECK (UnitPrice > 0)
);
GO

CREATE TABLE dbo.Customers
(
    CustomerId int            NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    FullName   nvarchar(120)  NOT NULL,
    Email      nvarchar(160)  NOT NULL,
    CityId     int            NOT NULL,
    CreatedAt  datetime2(0)   NOT NULL,
    CONSTRAINT UQ_Customers_Email UNIQUE (Email),
    CONSTRAINT FK_Customers_Cities FOREIGN KEY (CityId) REFERENCES dbo.Cities (CityId)
);
GO

CREATE TABLE dbo.Orders
(
    OrderId     int            NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    OrderNo     nvarchar(16)   NOT NULL,
    CustomerId  int            NOT NULL,
    CityId      int            NOT NULL,
    OrderDate   datetime2(0)   NOT NULL,
    TotalAmount decimal(18, 2) NOT NULL,
    Status      nvarchar(20)   NOT NULL,
    CONSTRAINT UQ_Orders_OrderNo UNIQUE (OrderNo),
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId),
    CONSTRAINT FK_Orders_Cities FOREIGN KEY (CityId) REFERENCES dbo.Cities (CityId),
    CONSTRAINT CK_Orders_TotalAmount CHECK (TotalAmount >= 0),
    CONSTRAINT CK_Orders_Status CHECK (Status IN (N'Tamamlandı', N'Kargoda', N'Beklemede', N'İptal', N'İade'))
);
GO

CREATE TABLE dbo.OrderDetails
(
    OrderDetailId int            NOT NULL CONSTRAINT PK_OrderDetails PRIMARY KEY,
    OrderId       int            NOT NULL,
    ProductId     int            NOT NULL,
    Quantity      int            NOT NULL,
    UnitPrice     decimal(18, 2) NOT NULL,
    LineTotal     decimal(18, 2) NOT NULL,
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (OrderId),
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (ProductId),
    CONSTRAINT CK_OrderDetails_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_OrderDetails_UnitPrice CHECK (UnitPrice > 0),
    CONSTRAINT CK_OrderDetails_LineTotal CHECK (LineTotal >= 0)
);
GO

CREATE TABLE dbo.Payments
(
    PaymentId int            NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
    OrderId   int            NOT NULL,
    Method    nvarchar(30)   NOT NULL,
    Amount    decimal(18, 2) NOT NULL,
    PaidAt    datetime2(0)   NOT NULL,
    CONSTRAINT UQ_Payments_OrderId UNIQUE (OrderId),
    CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (OrderId),
    CONSTRAINT CK_Payments_Amount CHECK (Amount >= 0),
    CONSTRAINT CK_Payments_Method CHECK (Method IN (N'Kredi Kartı', N'Banka Kartı', N'Havale/EFT', N'Kapıda Ödeme', N'Cüzdan'))
);
GO
