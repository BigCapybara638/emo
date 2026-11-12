using System.Data;
using System.Data.SqlClient;

namespace emo;

public static class DBHelper
{
    public static string connectionString = Environment.GetEnvironmentVariable("EMO_CONNECTION") ?? "Data Source=USERS\\SQLEXPRESS;Database=demo;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Application Name=\"SQL Server Management Studio\"";

    public static SqlConnection GetConnection() => new(connectionString);

    private static SqlCommand Command(SqlConnection conn, SqlTransaction? tx, string sql, params (string Name, object Value)[] args)
    {
        var cmd = new SqlCommand(sql, conn, tx);
        foreach (var (name, value) in args)
        {
            var parameter = cmd.Parameters.AddWithValue(name, value);
            if (value is DateTime) parameter.SqlDbType = SqlDbType.DateTime2;
        }
        return cmd;
    }

    public static DataTable Query(string sql, params (string Name, object Value)[] args)
    {
        using var conn = GetConnection();
        using var cmd = Command(conn, null, sql, args);
        using var adapter = new SqlDataAdapter(cmd);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }

    public static void Execute(string sql, params (string Name, object Value)[] args)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = Command(conn, null, sql, args);
        cmd.ExecuteNonQuery();
    }

    public static decimal GetPriceWithDiscount(int productId, decimal basePrice)
    {
        var (start, end) = Pricing.PreviousMonth(DateTime.Now);
        var rows = Query(@"SELECT TOP 1 1 FROM Order_stock os
            JOIN Orders_import o ON os.id_order=o.id JOIN Stock_items si ON os.id_stock_items=si.id
            WHERE si.id_tovar=@id AND o.[Дата_заказа]>=@start AND o.[Дата_заказа]<@end",
            ("@id", productId), ("@start", start), ("@end", end));
        return Pricing.Apply(basePrice, rows.Rows.Count == 0);
    }

    public static List<Product> LoadProducts(int? id = null)
    {
        var (start, end) = Pricing.PreviousMonth(DateTime.Now);
        return Query(@"SELECT p.*, c.[Категория], m.[Производство],
            (SELECT COALESCE(SUM(count),0) FROM Stock_items WHERE id_tovar=p.id) AS total_count,
            CASE WHEN EXISTS (SELECT 1 FROM Order_stock os
                JOIN Orders_import o ON os.id_order=o.id JOIN Stock_items si ON os.id_stock_items=si.id
                WHERE si.id_tovar=p.id AND o.[Дата_заказа]>=@start AND o.[Дата_заказа]<@end)
                THEN 0 ELSE 1 END AS discounted
            FROM Products p LEFT JOIN Categories c ON p.id_category=c.id
            LEFT JOIN Manufactures m ON p.id_manufactures=m.id WHERE @id IS NULL OR p.id=@id",
            ("@start", start), ("@end", end), ("@id", (object?)id ?? DBNull.Value))
            .AsEnumerable().Select(r => new Product
            {
                Id=Convert.ToInt32(r["id"]), Name=r["Наименование"].ToString()!,
                Image=r["Изображение"].ToString()!, Category=r["Категория"].ToString()!,
                Manufacture=r["Производство"].ToString()!, Composition=r["Состав"].ToString()!,
                Description=r["Описание"].ToString()!, BasePrice=Convert.ToDecimal(r["Цена"]),
                TotalCount=Convert.ToInt32(r["total_count"]), Discounted=Convert.ToInt32(r["discounted"])==1
            }).ToList();
    }

    public static ComboBoxItem[] LoadSizes(int productId) => Query(@"
        SELECT si.id, s.[Размер], si.count FROM Stock_items si JOIN Sizes s ON si.id_sizes=s.id
        WHERE si.id_tovar=@id ORDER BY s.[Размер]", ("@id", productId)).AsEnumerable()
        .Select(r => new ComboBoxItem(Convert.ToInt32(r["id"]), Convert.ToDecimal(r["Размер"]).ToString("0.#"), Convert.ToInt32(r["count"]))).ToArray();

    public static DataTable Users() => Query(@"SELECT CAST(id AS int) AS id,
        CONCAT([Фамилия],' ',[Имя],' ',[Отчество]) AS ФИО FROM Users ORDER BY ФИО");

    public static DataTable OrderItems(int id) => Query(@"SELECT si.id AS StockItemId,
        p.[Наименование] AS Товар, m.[Производство], s.[Размер], SUM(os.[Количество]) AS [Количество],
        os.[Цена] AS [Цена за ед.], os.[Скидка] AS [Скидка, %], SUM(os.[Количество])*os.[Цена] AS Сумма
        FROM Order_stock os JOIN Stock_items si ON os.id_stock_items=si.id
        JOIN Products p ON si.id_tovar=p.id JOIN Sizes s ON si.id_sizes=s.id
        LEFT JOIN Manufactures m ON p.id_manufactures=m.id WHERE os.id_order=@id
        GROUP BY si.id,p.[Наименование],m.[Производство],s.[Размер],os.[Цена],os.[Скидка]", ("@id", id));

    public static int SaveOrder(int? orderId, int userId, DateTime date, IReadOnlyList<CartItem> items)
    {
        if (items.Count == 0 || items.Any(i => i.Count <= 0 || i.Price < 0 || i.Discount is not (0 or 25)))
            throw new InvalidOperationException("Выберите товар, размер и положительное количество.");
        using var conn = GetConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        int id;
        if (orderId is { } existing)
        {
            using var check = Command(conn, tx, "SELECT id FROM Orders_import WITH (UPDLOCK) WHERE id=@id", ("@id", existing));
            if (check.ExecuteScalar() == null) throw new InvalidOperationException("Заказ уже удалён. Обновите список заказов.");
            id = existing;
        }
        else
        {
            using var create = Command(conn, tx, @"INSERT INTO Orders_import ([Дата_заказа],id_user)
                OUTPUT INSERTED.id VALUES (@date,@user)", ("@date", date), ("@user", userId));
            id = Convert.ToInt32(create.ExecuteScalar());
        }
        // Stock and price snapshots commit together; a stale cart cannot oversell.
        foreach (var item in items.OrderBy(i => i.StockItemId))
        {
            using var stock = Command(conn, tx, @"UPDATE Stock_items SET count=count-@count
                WHERE id=@stock AND count>=@count", ("@count", item.Count), ("@stock", item.StockItemId));
            if (stock.ExecuteNonQuery() != 1)
                throw new InvalidOperationException($"Недостаточно товара «{item.ProductName}», размер {item.SizeName}. Обновите каталог и уменьшите количество.");
            using var line = Command(conn, tx, @"IF EXISTS (SELECT 1 FROM Order_stock
                WHERE id_order=@order AND id_stock_items=@stock AND ([Цена]<>@price OR [Скидка]<>@discount))
                THROW 50002, N'Эта позиция уже есть в заказе по другой цене. Оформите её отдельным заказом.', 1;
                UPDATE Order_stock SET [Количество]=[Количество]+@count WHERE id_order=@order AND id_stock_items=@stock;
                IF @@ROWCOUNT=0 INSERT INTO Order_stock (id_order,id_stock_items,[Количество],[Цена],[Скидка])
                    VALUES (@order,@stock,@count,@price,@discount)", ("@order", id), ("@stock", item.StockItemId),
                ("@count", item.Count), ("@price", item.Price), ("@discount", item.Discount));
            line.ExecuteNonQuery();
        }
        tx.Commit();
        return id;
    }

    public static void DeleteOrder(int id, int? stockId = null, decimal price = 0, int discount = 0)
    {
        using var conn = GetConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        using var cmd = Command(conn, tx, @"SELECT id FROM Orders_import WITH (UPDLOCK) WHERE id=@id;
            UPDATE si SET count=count+r.n FROM Stock_items si JOIN
                (SELECT id_stock_items,SUM([Количество]) n FROM Order_stock
                 WHERE id_order=@id AND (@stock IS NULL OR (id_stock_items=@stock AND [Цена]=@price AND [Скидка]=@discount))
                 GROUP BY id_stock_items) r ON si.id=r.id_stock_items;
            DELETE FROM Order_stock WHERE id_order=@id
                AND (@stock IS NULL OR (id_stock_items=@stock AND [Цена]=@price AND [Скидка]=@discount));
            DELETE FROM Orders_import WHERE id=@id AND NOT EXISTS (SELECT 1 FROM Order_stock WHERE id_order=@id)",
            ("@id", id), ("@stock", (object?)stockId ?? DBNull.Value), ("@price", price), ("@discount", discount));
        cmd.ExecuteNonQuery();
        tx.Commit();
    }

        public static UserInfo GetUserByLogin(string login)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string q = @"SELECT u.id, u.[Фамилия], u.[Имя], u.[Отчество], u.id_role
                     FROM Users u
                     WHERE u.[Логин] = @login";
                using (var cmd = new SqlCommand(q, conn))
                {
                    cmd.Parameters.AddWithValue("@login", login);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new UserInfo
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                LastName = reader["Фамилия"].ToString(),
                                FirstName = reader["Имя"].ToString(),
                                MiddleName = reader["Отчество"]?.ToString() ?? "",
                                RoleId = Convert.ToInt32(reader["id_role"])
                            };
                        }
                    }
                }
            }
            return null;
        }

    public class UserInfo
    {
        public int Id { get; set; }
        public string LastName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public int RoleId { get; set; }
    }
}
