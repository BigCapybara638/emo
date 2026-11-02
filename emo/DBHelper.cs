using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient; // Для MS SQL Server

namespace emo
{
    public static class DBHelper
    {
        private static string connectionString = "Server=localhost;Database=shoe_shop;Trusted_Connection=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

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
                    FROM Order_items oi
                    JOIN Orders o ON oi.id_order = o.id
                    JOIN Stock_items si ON oi.id_stock_item = si.id
                    WHERE si.id_product = @pid 
                      AND o.order_date >= @start 
                      AND o.order_date <= @end";

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
                        p.image, 
                        c.category, 
                        p.name, 
                        m.manufacture, 
                        p.composition, 
                        p.price,
                        COALESCE(SUM(si.count), 0) AS total_count
                    FROM Products p
                    LEFT JOIN Categories c ON p.id_category = c.id
                    LEFT JOIN Manufactures m ON p.id_manufacture = m.id
                    LEFT JOIN Stock_items si ON p.id = si.id_product
                    GROUP BY p.id, p.image, c.category, p.name, m.manufacture, p.composition, p.price";

                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string image = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        string category = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        string name = reader.GetString(3);
                        string manufacture = reader.IsDBNull(4) ? "" : reader.GetString(4);
                        string composition = reader.IsDBNull(5) ? "" : reader.GetString(5);
                        decimal basePrice = reader.GetDecimal(6);
                        int totalCount = Convert.ToInt32(reader.GetInt64(7));

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
