$ErrorActionPreference = 'Stop'
$testDatabase = 'emo_validation_import_' + (Get-Date -Format 'yyyyMMddHHmmss')
$connection = New-Object System.Data.SqlClient.SqlConnection('Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True')
$connection.Open()
function Execute-Sql([string]$sql) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 120
    try { [void]$command.ExecuteNonQuery() } finally { $command.Dispose() }
}
function Scalar-Sql([string]$sql) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    try { return $command.ExecuteScalar() } finally { $command.Dispose() }
}
$script = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../database/import.sql')).Replace("N'demo'", "N'$testDatabase'").Replace('[demo]', "[$testDatabase]")
function Import-Database {
    foreach ($batch in [regex]::Split($script, '(?m)^GO\s*$')) {
        if ($batch.Trim()) { Execute-Sql $batch }
    }
}
try {
    Import-Database
    if ((Scalar-Sql 'SELECT COUNT(*) FROM Products') -ne 31) { throw 'Initial products missing' }
    Execute-Sql 'SELECT * INTO #orders FROM Order_stock; SELECT * INTO #stock FROM Stock_items;'
    Import-Database
    if ((Scalar-Sql 'SELECT COUNT(*) FROM (SELECT * FROM #orders EXCEPT SELECT * FROM Order_stock) x') -ne 0) { throw 'Rerun changed orders' }
    if ((Scalar-Sql 'SELECT COUNT(*) FROM (SELECT * FROM #stock EXCEPT SELECT * FROM Stock_items) x') -ne 0) { throw 'Rerun changed stock' }
    Execute-Sql "EXEC sp_rename 'Products', 'Products$'; EXEC sp_rename 'Order_stock', 'Order_stock$'; EXEC sp_rename 'dbo.Products$.[Наименование]', 'Наименование товара', 'COLUMN';"
    Import-Database
    if ((Scalar-Sql 'SELECT COUNT(*) FROM Products') -ne 31) { throw 'Migration lost products' }
    if ((Scalar-Sql 'SELECT COUNT(*) FROM (SELECT * FROM #orders EXCEPT SELECT * FROM Order_stock) x') -ne 0) { throw 'Migration changed prices' }
    Execute-Sql 'CREATE TABLE [Conflict$] (id int); CREATE TABLE Conflict (id int);'
    $rejected = $false
    try { Import-Database } catch { if ($_.Exception.ToString().Contains('Table rename collision')) { $rejected = $true } else { throw } }
    if (!$rejected) { throw 'Rename collision was not rejected' }
    Execute-Sql 'DROP TABLE [Conflict$]; DROP TABLE Conflict;'
    Write-Output "PASS fresh import, repeat import, legacy migration and collision rollback: $testDatabase"
} finally { $connection.Dispose() }
