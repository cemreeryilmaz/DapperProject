/*
    SalesPulse verisini, projenin kullandığı DapperProjectDb içine taşır.
    Products ve Categories tablolarının mevcut kolonları korunur.
*/
USE DapperProjectDb;
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Payments', N'U') IS NOT NULL DROP TABLE dbo.Payments;
IF OBJECT_ID(N'dbo.OrderDetails', N'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DROP TABLE dbo.Customers;
IF OBJECT_ID(N'dbo.Cities', N'U') IS NOT NULL DROP TABLE dbo.Cities;
GO

DELETE FROM dbo.Products;
DELETE FROM dbo.Categories;
GO

SET IDENTITY_INSERT dbo.Categories ON;
INSERT INTO dbo.Categories (CategoryId, Name)
SELECT CategoryId, Name
FROM SalesPulseDb.dbo.Categories;
SET IDENTITY_INSERT dbo.Categories OFF;
GO

SET IDENTITY_INSERT dbo.Products ON;
INSERT INTO dbo.Products (ProductId, Name, Price, Stock, CategoryId, IsActive, ImageUrl)
SELECT ProductId, Name, UnitPrice, 0, CategoryId, 1, N''
FROM SalesPulseDb.dbo.Products;
SET IDENTITY_INSERT dbo.Products OFF;
GO

CREATE TABLE dbo.Cities
(
    CityId int NOT NULL CONSTRAINT PK_Cities PRIMARY KEY,
    Name nvarchar(50) NOT NULL,
    Region nvarchar(40) NOT NULL
);

CREATE TABLE dbo.Customers
(
    CustomerId int NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    FullName nvarchar(120) NOT NULL,
    Email nvarchar(160) NOT NULL,
    CityId int NOT NULL,
    CreatedAt datetime2(0) NOT NULL
);

CREATE TABLE dbo.Orders
(
    OrderId int NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    OrderNo nvarchar(16) NOT NULL,
    CustomerId int NOT NULL,
    CityId int NOT NULL,
    OrderDate datetime2(0) NOT NULL,
    TotalAmount decimal(18, 2) NOT NULL,
    Status nvarchar(20) NOT NULL
);

CREATE TABLE dbo.OrderDetails
(
    OrderDetailId int NOT NULL CONSTRAINT PK_OrderDetails PRIMARY KEY,
    OrderId int NOT NULL,
    ProductId int NOT NULL,
    Quantity int NOT NULL,
    UnitPrice decimal(18, 2) NOT NULL,
    LineTotal decimal(18, 2) NOT NULL
);

CREATE TABLE dbo.Payments
(
    PaymentId int NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
    OrderId int NOT NULL,
    Method nvarchar(30) NOT NULL,
    Amount decimal(18, 2) NOT NULL,
    PaidAt datetime2(0) NOT NULL
);
GO

INSERT INTO dbo.Cities (CityId, Name, Region)
SELECT CityId, Name, Region FROM SalesPulseDb.dbo.Cities;

INSERT INTO dbo.Customers WITH (TABLOCK) (CustomerId, FullName, Email, CityId, CreatedAt)
SELECT CustomerId, FullName, Email, CityId, CreatedAt FROM SalesPulseDb.dbo.Customers;

INSERT INTO dbo.Orders WITH (TABLOCK) (OrderId, OrderNo, CustomerId, CityId, OrderDate, TotalAmount, Status)
SELECT OrderId, OrderNo, CustomerId, CityId, OrderDate, TotalAmount, Status FROM SalesPulseDb.dbo.Orders;

INSERT INTO dbo.OrderDetails WITH (TABLOCK) (OrderDetailId, OrderId, ProductId, Quantity, UnitPrice, LineTotal)
SELECT OrderDetailId, OrderId, ProductId, Quantity, UnitPrice, LineTotal FROM SalesPulseDb.dbo.OrderDetails;

INSERT INTO dbo.Payments WITH (TABLOCK) (PaymentId, OrderId, Method, Amount, PaidAt)
SELECT PaymentId, OrderId, Method, Amount, PaidAt FROM SalesPulseDb.dbo.Payments;
GO

ALTER TABLE dbo.Customers ADD CONSTRAINT UQ_Customers_Email UNIQUE (Email);
ALTER TABLE dbo.Customers ADD CONSTRAINT FK_Customers_Cities FOREIGN KEY (CityId) REFERENCES dbo.Cities (CityId);
ALTER TABLE dbo.Orders ADD CONSTRAINT UQ_Orders_OrderNo UNIQUE (OrderNo);
ALTER TABLE dbo.Orders ADD CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId);
ALTER TABLE dbo.Orders ADD CONSTRAINT FK_Orders_Cities FOREIGN KEY (CityId) REFERENCES dbo.Cities (CityId);
ALTER TABLE dbo.OrderDetails ADD CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (OrderId);
ALTER TABLE dbo.OrderDetails ADD CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (ProductId);
ALTER TABLE dbo.Payments ADD CONSTRAINT UQ_Payments_OrderId UNIQUE (OrderId);
ALTER TABLE dbo.Payments ADD CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (OrderId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_OrderDate_Status' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_OrderDate_Status ON dbo.Orders (OrderDate, Status) INCLUDE (TotalAmount, CustomerId, CityId, OrderNo);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_City_OrderDate' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_City_OrderDate ON dbo.Orders (CityId, OrderDate) INCLUDE (TotalAmount, Status, CustomerId, OrderNo);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_Customer_OrderDate' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_Customer_OrderDate ON dbo.Orders (CustomerId, OrderDate) INCLUDE (TotalAmount, Status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Customers_FullName' AND object_id = OBJECT_ID(N'dbo.Customers'))
    CREATE INDEX IX_Customers_FullName ON dbo.Customers (FullName) INCLUDE (CityId, Email);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderDetails_OrderId' AND object_id = OBJECT_ID(N'dbo.OrderDetails'))
    CREATE INDEX IX_OrderDetails_OrderId ON dbo.OrderDetails (OrderId) INCLUDE (ProductId, Quantity, UnitPrice, LineTotal);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderDetails_ProductId' AND object_id = OBJECT_ID(N'dbo.OrderDetails'))
    CREATE INDEX IX_OrderDetails_ProductId ON dbo.OrderDetails (ProductId) INCLUDE (OrderId, Quantity, LineTotal);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Payments_OrderId_Cover' AND object_id = OBJECT_ID(N'dbo.Payments'))
    CREATE INDEX IX_Payments_OrderId_Cover ON dbo.Payments (OrderId) INCLUDE (Method, Amount, PaidAt);
GO

SELECT t.name, SUM(p.rows) AS RowCountValue
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
GROUP BY t.name
ORDER BY t.name;
