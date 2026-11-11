SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_OrderDate_Status' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_OrderDate_Status
        ON dbo.Orders (OrderDate, Status)
        INCLUDE (TotalAmount, CustomerId, CityId, OrderNo);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_City_OrderDate' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_City_OrderDate
        ON dbo.Orders (CityId, OrderDate)
        INCLUDE (TotalAmount, Status, CustomerId, OrderNo);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_Customer_OrderDate' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_Customer_OrderDate
        ON dbo.Orders (CustomerId, OrderDate)
        INCLUDE (TotalAmount, Status);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Customers_FullName' AND object_id = OBJECT_ID(N'dbo.Customers'))
    CREATE INDEX IX_Customers_FullName
        ON dbo.Customers (FullName)
        INCLUDE (CityId, Email);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Customers_CityId' AND object_id = OBJECT_ID(N'dbo.Customers'))
    CREATE INDEX IX_Customers_CityId
        ON dbo.Customers (CityId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderDetails_OrderId' AND object_id = OBJECT_ID(N'dbo.OrderDetails'))
    CREATE INDEX IX_OrderDetails_OrderId
        ON dbo.OrderDetails (OrderId)
        INCLUDE (ProductId, Quantity, UnitPrice, LineTotal);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderDetails_ProductId' AND object_id = OBJECT_ID(N'dbo.OrderDetails'))
    CREATE INDEX IX_OrderDetails_ProductId
        ON dbo.OrderDetails (ProductId)
        INCLUDE (OrderId, Quantity, LineTotal);
GO

IF COL_LENGTH(N'dbo.Products', N'UnitPrice') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_CategoryId' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_CategoryId
        ON dbo.Products (CategoryId)
        INCLUDE (Name, UnitPrice);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Payments_OrderId_Cover' AND object_id = OBJECT_ID(N'dbo.Payments'))
    CREATE INDEX IX_Payments_OrderId_Cover
        ON dbo.Payments (OrderId)
        INCLUDE (Method, Amount, PaidAt);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_Pending' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_Pending
        ON dbo.Orders (OrderDate DESC, OrderId DESC)
        INCLUDE (OrderNo, TotalAmount)
        WHERE Status = N'Beklemede';
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'NCCI_Orders_Analytics' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE NONCLUSTERED COLUMNSTORE INDEX NCCI_Orders_Analytics
        ON dbo.Orders (OrderDate, Status, TotalAmount, CustomerId, CityId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'NCCI_OrderDetails_Analytics' AND object_id = OBJECT_ID(N'dbo.OrderDetails'))
    CREATE NONCLUSTERED COLUMNSTORE INDEX NCCI_OrderDetails_Analytics
        ON dbo.OrderDetails (OrderId, ProductId, Quantity, LineTotal);
GO
