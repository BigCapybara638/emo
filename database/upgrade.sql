-- Run once in the existing demo database (also after create.sql).
SET XACT_ABORT ON;
GO
IF COL_LENGTH('Products', 'Наименование') IS NULL AND COL_LENGTH('Products', 'Наименование товара') IS NOT NULL
    EXEC sys.sp_rename N'dbo.Products.[Наименование товара]', N'Наименование', 'COLUMN';
IF COL_LENGTH('Orders_import', 'Дата_заказа') IS NULL AND COL_LENGTH('Orders_import', 'Дата заказа') IS NOT NULL
    EXEC sys.sp_rename N'dbo.Orders_import.[Дата заказа]', N'Дата_заказа', 'COLUMN';
GO
IF COL_LENGTH('Order_stock', 'Цена') IS NULL
    ALTER TABLE Order_stock ADD [Цена] decimal(18,2) NULL;
IF COL_LENGTH('Order_stock', 'Скидка') IS NULL
    ALTER TABLE Order_stock ADD [Скидка] int NOT NULL CONSTRAINT DF_OrderDiscount DEFAULT 0 WITH VALUES;
GO
-- Legacy orders have no price snapshot. Reconstruct from the available history.
UPDATE os SET [Цена] = ROUND(p.[Цена] * (1 - d.discount / 100.0), 2), [Скидка] = d.discount
FROM Order_stock os
JOIN Orders_import o ON os.id_order = o.id
JOIN Stock_items si ON os.id_stock_items = si.id
JOIN Products p ON si.id_tovar = p.id
CROSS APPLY (SELECT DATEFROMPARTS(YEAR(o.[Дата_заказа]), MONTH(o.[Дата_заказа]), 1) AS month_start) boundary
CROSS APPLY (SELECT CASE WHEN EXISTS (
    SELECT 1 FROM Order_stock previous
    JOIN Orders_import po ON previous.id_order = po.id
    JOIN Stock_items ps ON previous.id_stock_items = ps.id
    WHERE ps.id_tovar = p.id
      AND po.[Дата_заказа] >= CASE WHEN boundary.month_start = DATEFROMPARTS(1,1,1)
          THEN boundary.month_start ELSE DATEADD(month, -1, boundary.month_start) END
      AND po.[Дата_заказа] < boundary.month_start
) THEN 0 ELSE 25 END AS discount) d
WHERE os.[Цена] IS NULL;
GO
ALTER TABLE Order_stock ALTER COLUMN [Цена] decimal(18,2) NOT NULL;
