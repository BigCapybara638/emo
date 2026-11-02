using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient; // Для MS SQL Server

namespace emo
{
    public static class DBHelper
    {
        private static string connectionString = "Data Source=USERS\\SQLEXPRESS;Database=demo;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Application Name=\"SQL Server Management Studio\"";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        // hui

        // Алгоритм расчёта цены со скидкой 25%
        public static decimal GetPriceWithDiscount(int productId, decimal basePrice)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                DateTime now = DateTime.Now;
                DateTime firstDayCurrent = new DateTime(now.Year, now.Month, 1);
                DateTime lastDayPrev = firstDayCurrent.AddDays(-1);
                DateTime firstDayPrev = new DateTime(lastDayPrev.Year, lastDayPrev.Month, 1);

                string query = @"
                    SELECT COUNT(*) 
                    FROM Order_stock$ os
                    JOIN Orders_import$ o ON os.id_order = o.id
                    JOIN Stock_items$ si ON os.id_stock_items = si.id
                    WHERE si.id_tovar = @pid 
                      AND o.[Дата заказа] >= @start 
                      AND o.[Дата заказа] <= @end";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", productId);
                    cmd.Parameters.AddWithValue("@start", firstDayPrev);
                    cmd.Parameters.AddWithValue("@end", lastDayPrev);

                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    if (count == 0)
                        return Math.Round(basePrice * 0.75m, 2);
                    return basePrice;
                }
            }
        }

        // Получение списка товаров для главной формы
        public static List<Product> LoadProducts()
        {
            var list = new List<Product>();
            using (var conn = GetConnection())
            {
                conn.Open();
                string query = @"
            SELECT 
                p.id, 
                p.[Изображение], 
                c.[Категория], 
                p.[Наименование товара], 
                m.[Производство], 
                p.[Состав], 
                p.[Цена],
                COALESCE(SUM(si.count), 0) AS total_count
            FROM Products$ p
            LEFT JOIN Categories$ c ON p.id_category = c.id
            LEFT JOIN Manufactures$ m ON p.id_manufactures = m.id
            LEFT JOIN Stock_items$ si ON p.id = si.id_tovar
            GROUP BY p.id, p.[Изображение], c.[Категория], p.[Наименование товара], m.[Производство], p.[Состав], p.[Цена]";

                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = Convert.ToInt32(reader["id"]);
                        string image = reader["Изображение"] == DBNull.Value ? "" : reader["Изображение"].ToString();
                        string category = reader["Категория"] == DBNull.Value ? "" : reader["Категория"].ToString();
                        string name = reader["Наименование товара"] == DBNull.Value ? "" : reader["Наименование товара"].ToString();
                        string manufacture = reader["Производство"] == DBNull.Value ? "" : reader["Производство"].ToString();
                        string composition = reader["Состав"] == DBNull.Value ? "" : reader["Состав"].ToString();
                        // Цена может быть double/float в БД, поэтому Convert.ToDecimal
                        decimal basePrice = Convert.ToDecimal(reader["Цена"]);
                        // Количество из SUM может быть любым числовым типом
                        int totalCount = Convert.ToInt32(reader["total_count"]);

                        list.Add(new Product
                        {
                            Id = id,
                            Image = image,
                            Category = category,
                            Name = name,
                            Manufacture = manufacture,
                            Composition = composition,
                            BasePrice = basePrice,
                            Price = GetPriceWithDiscount(id, basePrice),
                            TotalCount = totalCount
                        });
                    }
                }
            }
            return list;
        }


        // Модель товара (не меняется)
        public class Product
        {
            public int Id { get; set; }
            public string Image { get; set; }
            public string Category { get; set; }
            public string Name { get; set; }
            public string Manufacture { get; set; }
            public string Composition { get; set; }
            public decimal BasePrice { get; set; }
            public decimal Price { get; set; }
            public int TotalCount { get; set; }
            public string CountText => TotalCount > 5 ? "много" : "мало";
        }
    }
}
