-- Run in the existing database BEFORE upgrade.sql. Keeps data and foreign keys.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (
        SELECT 1 FROM sys.tables old
        JOIN sys.tables other ON other.schema_id = old.schema_id
            AND other.name = REPLACE(old.name, N'$', N'') AND other.object_id <> old.object_id
        WHERE CHARINDEX(N'$', old.name) > 0
    ) OR EXISTS (
        SELECT 1 FROM sys.tables GROUP BY schema_id, REPLACE(name, N'$', N'') HAVING COUNT(*) > 1
    ) OR EXISTS (SELECT 1 FROM sys.tables WHERE name <> N'' AND REPLACE(name, N'$', N'') = N'')
        THROW 50001, 'Table rename collision. Resolve conflicting names before retrying.', 1;

    DECLARE @old nvarchar(517), @new sysname;
    DECLARE tables_to_rename CURSOR LOCAL FAST_FORWARD FOR
        SELECT QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name), REPLACE(name, N'$', N'')
        FROM sys.tables WHERE CHARINDEX(N'$', name) > 0;
    OPEN tables_to_rename;
    FETCH NEXT FROM tables_to_rename INTO @old, @new;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC sys.sp_rename @objname = @old, @newname = @new, @objtype = N'OBJECT';
        FETCH NEXT FROM tables_to_rename INTO @old, @new;
    END;
    CLOSE tables_to_rename;
    DEALLOCATE tables_to_rename;
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
