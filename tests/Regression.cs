using System.Data;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using emo;

internal static class Regression
{
    private static int passed;
    private static decimal Scalar(string sql) => Convert.ToDecimal(DBHelper.Query(sql).Rows[0][0]);
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        Console.WriteLine("PASS " + message);
        passed++;
    }

    private static void Reset() => DBHelper.Execute(@"DELETE FROM Order_stock; DELETE FROM Orders_import;
        UPDATE Stock_items SET count=20; UPDATE Products SET [Цена]=2190 WHERE id=1");
    private static CartItem Item(int stock = 1, int count = 2, decimal price = 1642.5m) => new()
        { StockItemId = stock, Count = count, Price = price, Discount = 25, ProductName = "Тест", SizeName = "20" };
    private static object? Call(object obj, string method, params object?[] args) => obj.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(obj, args);
    private static void Field(object obj, string name, object value) => obj.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(obj, value);
    private static T Control<T>(Window window, string name) => (T)window.FindName(name);

    [STAThread]
    static int Main()
    {
        DBHelper.connectionString = Environment.GetEnvironmentVariable("EMO_TEST_CONNECTION")
            ?? "Data Source=localhost;Database=emo_validation_20261112;Integrated Security=True;TrustServerCertificate=True";
        if (!new System.Data.SqlClient.SqlConnectionStringBuilder(DBHelper.connectionString).InitialCatalog.StartsWith("emo_validation_"))
            throw new Exception("Tests require an isolated emo_validation_ database.");
        try
        {
            var month = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            foreach (var (date, expected) in new[] { (month.AddSeconds(-1), 100m), (month.AddMonths(-1), 100m),
                (month, 75m), (month.AddMonths(-1).AddSeconds(-1), 75m) })
            {
                Reset();
                DBHelper.Execute(@"INSERT INTO Orders_import ([Дата заказа],id_user) VALUES (@date,3);
                    INSERT INTO Order_stock (id_order,id_stock_items,[Количество],[Цена],[Скидка])
                    SELECT MAX(id),1,1,100,0 FROM Orders_import", ("@date", date));
                Check(DBHelper.GetPriceWithDiscount(1, 100m) == expected, $"discount boundary {date:yyyy-MM-dd HH:mm:ss}");
            }
            Check(Pricing.PreviousMonth(new DateTime(2027, 1, 5)) == (new DateTime(2026, 12, 1), new DateTime(2027, 1, 1)), "January crosses year");
            Check(Pricing.PreviousMonth(new DateTime(2024, 3, 15)) == (new DateTime(2024, 2, 1), new DateTime(2024, 3, 1)), "leap February range");
            Check(Pricing.Apply(99.99m, true) == 74.99m, "discount rounds to kopecks");
            Check(new Product { BasePrice = 100, Discounted = true }.PriceText.Contains("25%"), "card labels discount");
            Reset();
            var p = DBHelper.LoadProducts(1).Single();
            int order = DBHelper.SaveOrder(null, 3, DateTime.Today, [Item(price: p.Price)]);
            var row = DBHelper.OrderItems(order).Rows[0];
            Check(Convert.ToDecimal(row["Цена за ед."]) == p.Price && Convert.ToInt32(row["Скидка, %"]) == 25, "stored price equals card");
            Check(Convert.ToDecimal(row["Сумма"]) == p.Price * 2 && Scalar("SELECT count FROM Stock_items WHERE id=1") == 18, "order total and stock");
            DBHelper.Execute("UPDATE Products SET [Цена]=9999 WHERE id=1");
            Check(Convert.ToDecimal(DBHelper.OrderItems(order).Rows[0]["Цена за ед."]) == p.Price, "price snapshot survives product repricing");
            DBHelper.SaveOrder(order, 3, DateTime.Today, [Item(count: 3)]);
            Check(Convert.ToInt32(DBHelper.OrderItems(order).Rows[0]["Количество"]) == 5, "repeated stock lines aggregate");
            DBHelper.DeleteOrder(order, 1, p.Price, 25);
            Check(Scalar("SELECT count FROM Stock_items WHERE id=1") == 20 && Scalar("SELECT COUNT(*) FROM Orders_import") == 0, "delete returns all duplicate quantities and removes empty order");
            Reset();
            DBHelper.SaveOrder(null, 3, DateTime.Today, [Item(), Item(stock: 2, count: 4)]);
            order = Convert.ToInt32(Scalar("SELECT MAX(id) FROM Orders_import"));
            DBHelper.DeleteOrder(order);
            Check(Scalar("SELECT SUM(count) FROM Stock_items WHERE id IN (1,2)") == 40, "whole-order deletion restores each size");
            Reset();
            bool failed = false;
            try { DBHelper.SaveOrder(null, 3, DateTime.Today, [Item(), Item(stock: 2, count: 999)]); }
            catch (InvalidOperationException) { failed = true; }
            Check(failed && Scalar("SELECT count FROM Stock_items WHERE id=1") == 20 && Scalar("SELECT COUNT(*) FROM Orders_import") == 0, "failed checkout rolls back header, lines and stock");
            failed = false;
            try { DBHelper.SaveOrder(null, 3, DateTime.Today, []); }
            catch (InvalidOperationException) { failed = true; }
            Check(failed, "empty order rejected");
            Reset();
            DBHelper.Execute("UPDATE Stock_items SET count=1 WHERE id=1");
            int successes = 0;
            Task.WaitAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
            {
                try { DBHelper.SaveOrder(null, 3, DateTime.Today, [Item(count: 1)]); Interlocked.Increment(ref successes); }
                catch (InvalidOperationException) { }
            })).ToArray());
            Check(successes == 1 && Scalar("SELECT count FROM Stock_items WHERE id=1") == 0, "concurrent checkouts cannot oversell");
            Check(DBHelper.GetUserByLogin("isivanov")?.RoleId == 1 && DBHelper.GetUserByLogin("missing-user") == null, "original login lookup and roles");
            DBHelper.Execute("CREATE TABLE [RenameConflict$] (id int); CREATE TABLE RenameConflict (id int)");
            bool collision = false;
            try { DBHelper.Execute(System.IO.File.ReadAllText("database/rename_tables.sql")); }
            catch (System.Data.SqlClient.SqlException ex) when (ex.Number == 50001) { collision = true; }
            finally { DBHelper.Execute("DROP TABLE [RenameConflict$]; DROP TABLE RenameConflict"); }
            Check(collision, "rename rejects conflicting table names without dropping either table");

            Reset();
            var app = new App();
            app.InitializeComponent();
            var window = new MainWindow();
            Field(window, "role", 2);
            Field(window, "userId", 3);
            Control<TextBlock>(window, "UserName").Text = "Сидорова Анна Дмитриевна";
            Field(window, "show", new Action(() => Call(window, "ShowCatalog", null, null)));
            Call(window, "ShowCatalog", null, null);
            Control<TextBox>(window, "Search").Text = "Кроссовки";
            var list = Control<ListBox>(window, "Products");
            Check(list.Items.Count > 0 && list.Items.Cast<Product>().All(x => x.Name.Contains("Кроссовки") || x.Description.Contains("Кроссовки")), "live catalog search");
            Snapshot(window, "catalog");
            list.SelectedIndex = 0;
            Check(Control<TabControl>(window, "Pages").SelectedIndex == 1 && Control<Button>(window, "BackButton").IsEnabled, "catalog click opens product with Back");
            Snapshot(window, "product");
            Call(window, "Back");
            Check(Control<TabControl>(window, "Pages").SelectedIndex == 0 && Control<TextBox>(window, "Search").Text == "Кроссовки", "Back restores catalog filter");
            list.SelectedIndex = 0;
            Control<TextBox>(window, "Quantity").Text = "2";
            Call(window, "BuyNow", null, null);
            var direct = (OrderDraft)typeof(MainWindow).GetField("order", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
            Check(Control<TabControl>(window, "Pages").SelectedIndex == 3 && direct.Items.Single().Count == 2,
                "buy now opens confirmation with selected quantity");
            order = DBHelper.SaveOrder(null, 3, DateTime.Today, [Item()]);
            Call(window, "ShowOrder", new OrderDraft { Id = order });
            Check(Control<Button>(window, "SaveButton").Visibility == Visibility.Collapsed && Control<Button>(window, "RemoveButton").Visibility == Visibility.Collapsed, "manager cannot edit date or delete positions");
            Field(window, "role", 1);
            Call(window, "ShowOrder", new OrderDraft { Id = order });
            Check(Control<DatePicker>(window, "OrderDate").IsEnabled && Control<Button>(window, "RemoveButton").Visibility == Visibility.Visible, "admin can edit existing order");
            Snapshot(window, "order");
            var grid = Control<DataGrid>(window, "Items");
            var priceColumn = grid.Columns.Single(c => c.Header.ToString() == "Цена за ед.");
            var priceText = ((TextBlock)priceColumn.GetCellContent(grid.Items[0])).Text;
            Check(decimal.TryParse(priceText, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.GetCultureInfo("ru-RU"), out var visiblePrice)
                && visiblePrice == 1642.5m, "order visibly renders its stored unit price");
            var draft = new OrderDraft { UserId = 3, ChooseUser = true };
            Call(window, "ShowOrder", draft);
            Check(Control<ComboBox>(window, "Customer").IsEnabled && Control<DatePicker>(window, "OrderDate").IsEnabled, "new staff order selects customer and date");
            Snapshot(window, "new-order");
            Call(window, "ChooseItem", null, null);
            Check(Control<TabControl>(window, "Pages").SelectedIndex == 0 && app.Windows.Count == 1,
                "new order reuses catalog without another window");
            Control<ListBox>(window, "Products").SelectedIndex = 0;
            var selected = (CartItem)Call(window, "SelectedItem", true)!;
            var pick = (Action<CartItem>)typeof(MainWindow).GetField("pick", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
            pick(selected);
            Check(draft.Items.Single().Price == selected.Price && Control<Button>(window, "BuyButton").Visibility == Visibility.Collapsed,
                "catalog selection adds quoted item to new-order draft");
            window.Close();
            bool onlyLogin = false;
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            timer.Tick += (_, _) =>
            {
                if (app.MainWindow is not LoginWindow login) return;
                onlyLogin = app.Windows.Cast<Window>().All(w => w is LoginWindow) && login.Owner == null;
                timer.Stop();
                login.DialogResult = false;
            };
            timer.Start();
            app.SignIn();
            Check(onlyLogin, "authorization opens without creating a catalog window");
            Console.WriteLine($"PASS {passed} checks");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine("FAIL " + ex); return 1; }
    }

    private static void Snapshot(Window window, string name)
    {
        var view = (FrameworkElement)window.Content;
        view.Measure(new Size(1080, 720));
        view.Arrange(new Rect(0, 0, 1080, 720));
        view.UpdateLayout();
        var bitmap = new RenderTargetBitmap(1080, 720, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(view);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        System.IO.Directory.CreateDirectory(".validation");
        using var stream = System.IO.File.Create($".validation/wpf-{name}.png");
        encoder.Save(stream);
    }
}
