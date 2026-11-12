-- Only run in an isolated emo_validation_ database with legacy table names.
IF DB_NAME() NOT LIKE 'emo[_]validation[_]%' THROW 50000, 'Test database required', 1;
SELECT t.object_id, t.name, SUM(p.rows) AS rows
INTO #before FROM sys.tables t JOIN sys.partitions p ON t.object_id=p.object_id AND p.index_id IN (0,1)
GROUP BY t.object_id,t.name;
SELECT object_id, referenced_object_id INTO #keys FROM sys.foreign_keys;
SELECT SUM([Цена]*[Количество]) AS total INTO #prices FROM Order_stock$;
:r database/rename_tables.sql
GO
:r database/rename_tables.sql
GO
IF EXISTS (SELECT 1 FROM #before b LEFT JOIN sys.tables t ON b.object_id=t.object_id
    WHERE t.object_id IS NULL OR t.name<>REPLACE(b.name,'$','')) THROW 50002, 'Names or object identities changed incorrectly', 1;
IF EXISTS (SELECT object_id,referenced_object_id FROM #keys EXCEPT SELECT object_id,referenced_object_id FROM sys.foreign_keys)
    THROW 50003, 'Foreign keys lost', 1;
IF EXISTS (SELECT 1 FROM #before b WHERE b.rows<>(SELECT SUM(rows) FROM sys.partitions p WHERE p.object_id=b.object_id AND p.index_id IN (0,1)))
    THROW 50004, 'Rows lost', 1;
IF (SELECT total FROM #prices)<>(SELECT SUM([Цена]*[Количество]) FROM Order_stock) THROW 50005, 'Prices changed', 1;
PRINT 'PASS rename preserves rows, prices, object identities and foreign keys; rerun is safe';
