CREATE OR ALTER PROCEDURE dbo.sp_GetDashboard
    @StartDate datetime2,
    @EndDate   datetime2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT_BIG(*) AS TotalOrders,
        CAST(ISNULL(SUM(CASE WHEN o.Status <> N'İptal' THEN o.TotalAmount END), 0) AS decimal(18, 2)) AS TotalRevenue,
        COUNT(DISTINCT o.CustomerId) AS TotalCustomers,
        CAST(ISNULL(AVG(CASE WHEN o.Status <> N'İptal' THEN o.TotalAmount END), 0) AS decimal(18, 2)) AS AverageBasket
    FROM dbo.Orders AS o
    WHERE o.OrderDate >= @StartDate
      AND o.OrderDate < @EndDate;

    SELECT
        YEAR(o.OrderDate) AS [Year],
        MONTH(o.OrderDate) AS [Month],
        COUNT_BIG(*) AS OrderCount,
        CAST(ISNULL(SUM(CASE WHEN o.Status <> N'İptal' THEN o.TotalAmount END), 0) AS decimal(18, 2)) AS Revenue
    FROM dbo.Orders AS o
    WHERE o.OrderDate >= @StartDate
      AND o.OrderDate < @EndDate
    GROUP BY YEAR(o.OrderDate), MONTH(o.OrderDate)
    ORDER BY [Year], [Month];

    SELECT TOP (10)
        c.Name AS CategoryName,
        CAST(SUM(od.LineTotal) AS decimal(18, 2)) AS Revenue,
        CAST(SUM(od.Quantity) AS bigint) AS Quantity
    FROM dbo.OrderDetails AS od
    INNER JOIN dbo.Orders AS o ON o.OrderId = od.OrderId
    INNER JOIN dbo.Products AS p ON p.ProductId = od.ProductId
    INNER JOIN dbo.Categories AS c ON c.CategoryId = p.CategoryId
    WHERE o.OrderDate >= @StartDate
      AND o.OrderDate < @EndDate
      AND o.Status <> N'İptal'
    GROUP BY c.CategoryId, c.Name
    ORDER BY Revenue DESC;

    SELECT TOP (10)
        ci.Name AS CityName,
        CAST(SUM(o.TotalAmount) AS decimal(18, 2)) AS Revenue,
        COUNT_BIG(*) AS OrderCount
    FROM dbo.Orders AS o
    INNER JOIN dbo.Cities AS ci ON ci.CityId = o.CityId
    WHERE o.OrderDate >= @StartDate
      AND o.OrderDate < @EndDate
      AND o.Status <> N'İptal'
    GROUP BY ci.CityId, ci.Name
    ORDER BY Revenue DESC;

    SELECT
        o.Status,
        COUNT_BIG(*) AS OrderCount
    FROM dbo.Orders AS o
    WHERE o.OrderDate >= @StartDate
      AND o.OrderDate < @EndDate
    GROUP BY o.Status
    ORDER BY OrderCount DESC;

    SELECT TOP (10)
        p.Name AS ProductName,
        c.Name AS CategoryName,
        CAST(SUM(od.Quantity) AS bigint) AS QuantitySold,
        CAST(SUM(od.LineTotal) AS decimal(18, 2)) AS Revenue
    FROM dbo.OrderDetails AS od
    INNER JOIN dbo.Orders AS o ON o.OrderId = od.OrderId
    INNER JOIN dbo.Products AS p ON p.ProductId = od.ProductId
    INNER JOIN dbo.Categories AS c ON c.CategoryId = p.CategoryId
    WHERE o.OrderDate >= @StartDate
      AND o.OrderDate < @EndDate
      AND o.Status <> N'İptal'
    GROUP BY p.ProductId, p.Name, c.Name
    ORDER BY QuantitySold DESC;

    SELECT TOP (10)
        p.Name AS ProductName,
        c.Name AS CategoryName,
        CAST(SUM(od.Quantity) AS bigint) AS QuantitySold,
        CAST(SUM(od.LineTotal) AS decimal(18, 2)) AS Revenue
    FROM dbo.OrderDetails AS od
    INNER JOIN dbo.Orders AS o ON o.OrderId = od.OrderId
    INNER JOIN dbo.Products AS p ON p.ProductId = od.ProductId
    INNER JOIN dbo.Categories AS c ON c.CategoryId = p.CategoryId
    WHERE o.OrderDate >= @StartDate
      AND o.OrderDate < @EndDate
      AND o.Status <> N'İptal'
    GROUP BY p.ProductId, p.Name, c.Name
    ORDER BY Revenue DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_OrderExplorer
    @LikePrefix    nvarchar(120) = NULL,
    @LikeWord      nvarchar(120) = NULL,
    @StartDate     datetime2 = NULL,
    @EndDate       datetime2 = NULL,
    @CityId        int = NULL,
    @Status        nvarchar(20) = NULL,
    @MinAmount     decimal(18, 2) = NULL,
    @MaxAmount     decimal(18, 2) = NULL,
    @SortColumn    nvarchar(30) = N'OrderDate',
    @SortDirection nvarchar(4) = N'DESC',
    @Offset        int = 0,
    @Fetch         int = 25
AS
BEGIN
    SET NOCOUNT ON;

    IF @Offset < 0 SET @Offset = 0;
    IF @Fetch NOT IN (25, 50, 100, 10000) SET @Fetch = 25;

    DECLARE @Sort nvarchar(40) = CASE @SortColumn
        WHEN N'OrderNo' THEN N'o.OrderNo'
        WHEN N'CustomerName' THEN N'c.FullName'
        WHEN N'CityName' THEN N'ci.Name'
        WHEN N'OrderDate' THEN N'o.OrderDate'
        WHEN N'TotalAmount' THEN N'o.TotalAmount'
        WHEN N'PaymentMethod' THEN N'p.Method'
        WHEN N'Status' THEN N'o.Status'
        ELSE N'o.OrderDate'
    END;

    DECLARE @Dir nvarchar(4) = CASE WHEN UPPER(ISNULL(@SortDirection, N'')) = N'ASC' THEN N'ASC' ELSE N'DESC' END;

    DECLARE @sql nvarchar(max) = N'
SELECT COUNT_BIG(*) AS TotalCount
FROM dbo.Orders AS o
INNER JOIN dbo.Customers AS c ON c.CustomerId = o.CustomerId
INNER JOIN dbo.Cities AS ci ON ci.CityId = o.CityId
INNER JOIN dbo.Payments AS p ON p.OrderId = o.OrderId
WHERE (@StartDate IS NULL OR o.OrderDate >= @StartDate)
  AND (@EndDate IS NULL OR o.OrderDate < @EndDate)
  AND (@CityId IS NULL OR o.CityId = @CityId)
  AND (@Status IS NULL OR o.Status = @Status)
  AND (@MinAmount IS NULL OR o.TotalAmount >= @MinAmount)
  AND (@MaxAmount IS NULL OR o.TotalAmount <= @MaxAmount)
  AND (
        @LikePrefix IS NULL
        OR o.OrderNo LIKE @LikePrefix
        OR c.FullName LIKE @LikePrefix
        OR c.FullName LIKE @LikeWord
      );

SELECT
    o.OrderId,
    o.OrderNo,
    c.FullName AS CustomerName,
    ci.Name AS CityName,
    o.OrderDate,
    o.TotalAmount,
    p.Method AS PaymentMethod,
    o.Status
FROM dbo.Orders AS o
INNER JOIN dbo.Customers AS c ON c.CustomerId = o.CustomerId
INNER JOIN dbo.Cities AS ci ON ci.CityId = o.CityId
INNER JOIN dbo.Payments AS p ON p.OrderId = o.OrderId
WHERE (@StartDate IS NULL OR o.OrderDate >= @StartDate)
  AND (@EndDate IS NULL OR o.OrderDate < @EndDate)
  AND (@CityId IS NULL OR o.CityId = @CityId)
  AND (@Status IS NULL OR o.Status = @Status)
  AND (@MinAmount IS NULL OR o.TotalAmount >= @MinAmount)
  AND (@MaxAmount IS NULL OR o.TotalAmount <= @MaxAmount)
  AND (
        @LikePrefix IS NULL
        OR o.OrderNo LIKE @LikePrefix
        OR c.FullName LIKE @LikePrefix
        OR c.FullName LIKE @LikeWord
      )
ORDER BY ' + @Sort + N' ' + @Dir + N', o.OrderId DESC
OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY;';

    EXEC sys.sp_executesql
        @sql,
        N'@LikePrefix nvarchar(120), @LikeWord nvarchar(120), @StartDate datetime2, @EndDate datetime2, @CityId int, @Status nvarchar(20), @MinAmount decimal(18,2), @MaxAmount decimal(18,2), @Offset int, @Fetch int',
        @LikePrefix, @LikeWord, @StartDate, @EndDate, @CityId, @Status, @MinAmount, @MaxAmount, @Offset, @Fetch;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetOrderDetail
    @OrderId int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        o.OrderId,
        o.OrderNo,
        o.OrderDate,
        o.TotalAmount,
        o.Status,
        c.FullName AS CustomerName,
        c.Email,
        ci.Name AS CityName,
        p.Method AS PaymentMethod,
        p.Amount AS PaidAmount,
        p.PaidAt
    FROM dbo.Orders AS o
    INNER JOIN dbo.Customers AS c ON c.CustomerId = o.CustomerId
    INNER JOIN dbo.Cities AS ci ON ci.CityId = o.CityId
    INNER JOIN dbo.Payments AS p ON p.OrderId = o.OrderId
    WHERE o.OrderId = @OrderId;

    SELECT
        pr.Name AS ProductName,
        cat.Name AS CategoryName,
        od.Quantity,
        od.UnitPrice,
        od.LineTotal
    FROM dbo.OrderDetails AS od
    INNER JOIN dbo.Products AS pr ON pr.ProductId = od.ProductId
    INNER JOIN dbo.Categories AS cat ON cat.CategoryId = pr.CategoryId
    WHERE od.OrderId = @OrderId
    ORDER BY od.OrderDetailId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetTopCustomers
    @StartDate datetime2,
    @EndDate   datetime2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (20)
        c.CustomerId,
        c.FullName AS CustomerName,
        ci.Name AS CityName,
        COUNT_BIG(*) AS OrderCount,
        CAST(SUM(o.TotalAmount) AS decimal(18, 2)) AS Revenue,
        CAST(AVG(o.TotalAmount) AS decimal(18, 2)) AS AverageBasket
    FROM dbo.Orders AS o
    INNER JOIN dbo.Customers AS c ON c.CustomerId = o.CustomerId
    INNER JOIN dbo.Cities AS ci ON ci.CityId = o.CityId
    WHERE o.OrderDate >= @StartDate
      AND o.OrderDate < @EndDate
      AND o.Status <> N'İptal'
    GROUP BY c.CustomerId, c.FullName, ci.Name
    HAVING COUNT_BIG(*) >= 2
    ORDER BY Revenue DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetRepeatCustomerRate
    @StartDate datetime2,
    @EndDate   datetime2
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH PerCustomer AS
    (
        SELECT
            o.CustomerId,
            COUNT_BIG(*) AS OrderCount
        FROM dbo.Orders AS o
        WHERE o.OrderDate >= @StartDate
          AND o.OrderDate < @EndDate
          AND o.Status <> N'İptal'
        GROUP BY o.CustomerId
    )
    SELECT
        COUNT_BIG(*) AS ActiveCustomers,
        ISNULL(SUM(CASE WHEN OrderCount >= 2 THEN 1 ELSE 0 END), 0) AS RepeatCustomers,
        CAST(
            100.0 * ISNULL(SUM(CASE WHEN OrderCount >= 2 THEN 1 ELSE 0 END), 0)
            / NULLIF(COUNT_BIG(*), 0) AS decimal(6, 2)
        ) AS RepeatRate
    FROM PerCustomer;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetPeriodComparison
    @StartDate     datetime2,
    @EndDate       datetime2,
    @PreviousStart datetime2,
    @PreviousEnd   datetime2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT_BIG(*) AS TotalOrders,
        CAST(ISNULL(SUM(CASE WHEN Status <> N'İptal' THEN TotalAmount END), 0) AS decimal(18, 2)) AS TotalRevenue,
        COUNT(DISTINCT CustomerId) AS ActiveCustomers,
        CAST(ISNULL(AVG(CASE WHEN Status <> N'İptal' THEN TotalAmount END), 0) AS decimal(18, 2)) AS AverageBasket
    FROM dbo.Orders
    WHERE OrderDate >= @StartDate
      AND OrderDate < @EndDate;

    SELECT
        COUNT_BIG(*) AS TotalOrders,
        CAST(ISNULL(SUM(CASE WHEN Status <> N'İptal' THEN TotalAmount END), 0) AS decimal(18, 2)) AS TotalRevenue,
        COUNT(DISTINCT CustomerId) AS ActiveCustomers,
        CAST(ISNULL(AVG(CASE WHEN Status <> N'İptal' THEN TotalAmount END), 0) AS decimal(18, 2)) AS AverageBasket
    FROM dbo.Orders
    WHERE OrderDate >= @PreviousStart
      AND OrderDate < @PreviousEnd;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetCategoryMonthly
    @StartDate datetime2,
    @EndDate   datetime2
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Ranked AS
    (
        SELECT TOP (8)
            c.CategoryId,
            c.Name AS CategoryName,
            CAST(SUM(od.LineTotal) AS decimal(18, 2)) AS Revenue
        FROM dbo.OrderDetails AS od
        INNER JOIN dbo.Orders AS o ON o.OrderId = od.OrderId
        INNER JOIN dbo.Products AS p ON p.ProductId = od.ProductId
        INNER JOIN dbo.Categories AS c ON c.CategoryId = p.CategoryId
        WHERE o.OrderDate >= @StartDate
          AND o.OrderDate < @EndDate
          AND o.Status <> N'İptal'
        GROUP BY c.CategoryId, c.Name
        ORDER BY Revenue DESC
    )
    SELECT
        r.CategoryName,
        YEAR(o.OrderDate) AS [Year],
        MONTH(o.OrderDate) AS [Month],
        CAST(SUM(od.LineTotal) AS decimal(18, 2)) AS Revenue,
        CAST(SUM(od.Quantity) AS bigint) AS Quantity
    FROM Ranked AS r
    INNER JOIN dbo.Products AS p ON p.CategoryId = r.CategoryId
    INNER JOIN dbo.OrderDetails AS od ON od.ProductId = p.ProductId
    INNER JOIN dbo.Orders AS o ON o.OrderId = od.OrderId
    WHERE o.OrderDate >= @StartDate
      AND o.OrderDate < @EndDate
      AND o.Status <> N'İptal'
    GROUP BY r.CategoryName, r.Revenue, YEAR(o.OrderDate), MONTH(o.OrderDate)
    ORDER BY r.Revenue DESC, [Year], [Month];
END
GO
