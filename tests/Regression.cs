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
            ?? "Data Source=localhost;Database=emo_validation_schema_20261112;Integrated Security=True;TrustServerCertificate=True";
        if (!new System.Data.SqlClient.SqlConnectionStringBuilder(DBHelper.connectionString).InitialCatalog.StartsWith("emo_validation_"))
            throw new Exception("Tests require an isolated emo_validation_ database.");
        try
        {
            Check(Scalar(@"SELECT COUNT(*) FROM sys.columns c JOIN sys.tables t ON c.object_id=t.object_id
                WHERE c.name='id' AND c.is_identity=1") == 9, "all nine id columns are identity");
            Check(Scalar(@"SELECT COUNT(*) FROM sys.index_columns i JOIN sys.key_constraints k
                ON k.parent_object_id=i.object_id AND k.unique_index_id=i.index_id
                WHERE k.name='PK_Order_stock'") == 2, "order lines have a two-column primary key");
            Reset();
            int earlyOrder = DBHelper.SaveOrder(null, 3, new DateTime(1, 1, 1), [Item()]);
            Check(Convert.ToDateTime(DBHelper.Query("SELECT Дата_заказа FROM Orders_import WHERE id=@id", ("@id", earlyOrder)).Rows[0][0]).Year == 1,
                "datetime2 date outside legacy datetime range is supported");
            var month = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            foreach (var (date, expected) in new[] { (month.AddSeconds(-1), 100m), (month.AddMonths(-1), 100m),
                (month, 75m), (month.AddMonths(-1).AddSeconds(-1), 75m) })
            {
                Reset();
                DBHelper.Execute(@"INSERT INTO Orders_import ([Дата_заказа],id_user) VALUES (@date,3);
                    INSERT INTO Order_stock (id_order,id_stock_items,[Количество],[Цена],[Скидка])
                    SELECT MAX(id),1,1,100,0 FROM Orders_import", ("@date", date));
                Check(DBHelper.GetPriceWithDiscount(1, 100m) == expected, $"discount boundary {date:yyyy-MM-dd HH:mm:ss}");
            }
            Check(Pricing.PreviousMonth(new DateTime(2027, 1, 5)) == (new DateTime(2026, 12, 1), new DateTime(2027, 1, 1)), "January crosses year");
            Check(Pricing.PreviousMonth(new DateTime(2024, 3, 15)) == (new DateTime(2024, 2, 1), new DateTime(2024, 3, 1)), "leap February range");
            Check(Pricing.Apply(99.99m, true) == 74.99m, "discount rounds to kopecks");
            Check(new Product { BasePrice = 100, Discounted = true }.PriceText.Contains("25%"), "card labels discount");
            Check(!new Product { BasePrice = 100, Discounted = true }.PriceText.Contains("было"), "card omits previous price");
            Check(new Product { TotalCount = 3 }.LowStock && !new Product { TotalCount = 4 }.LowStock
                && new Product { TotalCount = 5 }.CountText == "мало" && new Product { TotalCount = 6 }.CountText == "много",
                "catalog uses exact low-stock and quantity-label thresholds");
            Check(Product.ResourcePath("missing-test-photo.png").EndsWith("picture.png"), "missing photo uses resource placeholder");
            Reset();
            var p = DBHelper.LoadProducts(1).Single();
            int order = DBHelper.SaveOrder(null, 3, DateTime.Today, [Item(price: p.Price)]);
            var row = DBHelper.OrderItems(order).Rows[0];
            Check(Convert.ToDecimal(row["Цена"]) == p.Price && Convert.ToInt32(row["Скидка"]) == 25, "stored price equals card");
            Check(Convert.ToDecimal(row["Сумма"]) == p.Price * 2 && Scalar("SELECT count FROM Stock_items WHERE id=1") == 18, "order total and stock");
            DBHelper.ChangeQuantity(order, 1, 4);
            Check(Scalar("SELECT count FROM Stock_items WHERE id=1") == 16
                && Convert.ToDecimal(DBHelper.OrderItems(order).Rows[0]["Сумма"]) == p.Price * 4,
                "increase quantity reserves only the difference and updates total");
            DBHelper.ChangeQuantity(order, 1, 2);
            Check(Scalar("SELECT count FROM Stock_items WHERE id=1") == 18
                && Convert.ToDecimal(DBHelper.OrderItems(order).Rows[0]["Цена"]) == p.Price,
                "decrease quantity returns stock and preserves price");
            bool overstock = false;
            try { DBHelper.ChangeQuantity(order, 1, 100); }
            catch (System.Data.SqlClient.SqlException ex) when (ex.Number == 50003) { overstock = true; }
            Check(overstock && Scalar("SELECT count FROM Stock_items WHERE id=1") == 18
                && Convert.ToInt32(DBHelper.OrderItems(order).Rows[0]["Количество"]) == 2,
                "invalid quantity rolls back stock and order line");
            Check(DBHelper.OrderItems(order).Columns.Contains("Наименование") && !DBHelper.OrderItems(order).Columns.Contains("Товар"),
                "order columns use schema names");
            DBHelper.Execute("UPDATE Products SET [Цена]=9999 WHERE id=1");
            Check(Convert.ToDecimal(DBHelper.OrderItems(order).Rows[0]["Цена"]) == p.Price, "price snapshot survives product repricing");
            DBHelper.SaveOrder(order, 3, DateTime.Today, [Item(count: 3)]);
            Check(Convert.ToInt32(DBHelper.OrderItems(order).Rows[0]["Количество"]) == 5, "repeated stock lines aggregate");
            Check(Scalar($"SELECT COUNT(*) FROM Order_stock WHERE id_order={order} AND id_stock_items=1") == 1,
                "repeated product uses one composite-key row");
            bool differentPrice = false;
            try { DBHelper.SaveOrder(order, 3, DateTime.Today, [Item(count: 1, price: 999)]); }
            catch (System.Data.SqlClient.SqlException ex) when (ex.Number == 50002) { differentPrice = true; }
            Check(differentPrice && Scalar("SELECT count FROM Stock_items WHERE id=1") == 15
                && Convert.ToDecimal(DBHelper.OrderItems(order).Rows[0]["Цена"]) == p.Price,
                "different quote cannot overwrite stored price or reduce stock");
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
            Check(Elements(window).OfType<Control>().Where(c => c is Button or TextBox or ComboBox or DatePicker)
                .All(c => c.ToolTip != null), "all interactive main-window controls have hover descriptions");
            Field(window, "role", 2);
            Field(window, "userId", 3);
            Control<TextBlock>(window, "UserName").Text = "Сидорова Анна Дмитриевна";
            Field(window, "show", new Action(() => Call(window, "ShowCatalog", null, null)));
            Call(window, "ShowCatalog", null, null);
            DBHelper.Execute("INSERT INTO Categories ([Категория]) VALUES (N'Тестовая категория без товаров')");
            try
            {
                Call(window, "ShowCatalog", null, null);
                Check(Control<ComboBox>(window, "Category").Items.Contains("Тестовая категория без товаров"),
                    "category filter includes categories with no products");
                Control<ComboBox>(window, "Category").SelectedItem = "Тестовая категория без товаров";
                Call(window, "ShowCatalog", null, null);
                Check(Control<ListBox>(window, "Products").Items.Count == 0
                    && (string)Control<ComboBox>(window, "Category").SelectedItem == "Тестовая категория без товаров",
                    "empty-category filter persists across catalog reloads");
            }
            finally { DBHelper.Execute("DELETE FROM Categories WHERE [Категория]=N'Тестовая категория без товаров'"); }
            Call(window, "ShowCatalog", null, null);
            Control<TextBox>(window, "Search").Text = "Кроссовки";
            var list = Control<ListBox>(window, "Products");
            Check(list.Items.Count > 0 && list.Items.Cast<Product>().All(x => x.Name.Contains("Кроссовки") || x.Description.Contains("Кроссовки")), "live catalog search");
            Control<ComboBox>(window, "Category").SelectedItem = "Детская обувь";
            Control<ComboBox>(window, "Sort").SelectedIndex = 1;
            Check(list.Items.Cast<Product>().All(x => x.Category == "Детская обувь")
                && list.Items.Cast<Product>().Select(x => x.Price).SequenceEqual(list.Items.Cast<Product>().Select(x => x.Price).OrderDescending()),
                "search, category filter and descending price sort work together");
            Control<ComboBox>(window, "Category").SelectedItem = "Все категории";
            Control<ComboBox>(window, "Sort").SelectedIndex = 0;
            Snapshot(window, "catalog");
            list.SelectedIndex = 0;
            Check(Control<TabControl>(window, "Pages").SelectedIndex == 1 && Control<Button>(window, "BackButton").IsEnabled, "catalog click opens product with Back");
            Control<TextBox>(window, "Quantity").Text = "999999";
            Check(Control<Button>(window, "AddButton").IsEnabled, "overstock quantity leaves add action available for error feedback");
            bool quantityRejected = false;
            try { Call(window, "SelectedItem", true); }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { quantityRejected = true; }
            Check(quantityRejected, "overstock quantity is rejected before adding to cart");
            Control<TextBox>(window, "Quantity").Text = "0";
            Check(!Control<Button>(window, "AddButton").IsEnabled,
                "zero quantity blocks ordering immediately");
            Control<TextBox>(window, "Quantity").Text = "2147483648";
            Check(!Control<Button>(window, "AddButton").IsEnabled, "quantity overflow blocks ordering");
            Control<TextBox>(window, "Quantity").Text = "1";
            var paste = new DataObjectPastingEventArgs(new DataObject(DataFormats.UnicodeText, "-2"), false, DataFormats.UnicodeText);
            Call(window, "PasteDigits", Control<TextBox>(window, "Quantity"), paste);
            Check(paste.CommandCancelled, "non-numeric quantity paste is rejected");
            Snapshot(window, "product");
            Call(window, "Back");
            Check(Control<TabControl>(window, "Pages").SelectedIndex == 0 && Control<TextBox>(window, "Search").Text == "Кроссовки", "Back restores catalog filter");
            list.SelectedIndex = 0;
            Control<TextBox>(window, "Quantity").Text = "2";
            Field(window, "cart", new List<CartItem> { Item(count: 2) });
            Call(window, "OpenCart", null, null);
            var direct = (OrderDraft)typeof(MainWindow).GetField("order", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
            Check(Control<TabControl>(window, "Pages").SelectedIndex == 3 && direct.Items.Single().Count == 2,
                "cart opens confirmation with selected quantity");
            order = DBHelper.SaveOrder(null, 3, DateTime.Today, [Item()]);
            Call(window, "ShowOrder", new OrderDraft { Id = order });
            Check(Control<Button>(window, "SaveButton").Visibility == Visibility.Collapsed && Control<Button>(window, "RemoveButton").Visibility == Visibility.Collapsed, "manager cannot edit date or delete positions");
            Check(Control<WrapPanel>(window, "QuantityEditor").Visibility == Visibility.Collapsed,
                "manager cannot change quantity in confirmed orders");
            Field(window, "role", 1);
            Call(window, "ShowOrder", new OrderDraft { Id = order });
            Check(Control<DatePicker>(window, "OrderDate").IsEnabled && Control<Button>(window, "RemoveButton").Visibility == Visibility.Visible, "admin can edit existing order");
            var changedDate = DateTime.Today.AddDays(-4);
            Control<DatePicker>(window, "OrderDate").SelectedDate = changedDate;
            Check(Convert.ToDateTime(DBHelper.Query("SELECT Дата_заказа FROM Orders_import WHERE id=@id", ("@id", order)).Rows[0][0]) == changedDate,
                "admin date selection immediately persists without a save action");
            Snapshot(window, "order");
            var grid = Control<DataGrid>(window, "Items");
            Check(!grid.Columns.Any(c => c.Header.ToString() == "Скидка"), "order table presents final price without discount column");
            Check(new[] { "Наименование", "Производство", "Размер", "Количество", "Цена" }
                .All(name => grid.Columns.Any(c => c.Header.ToString() == name)), "visible line headers match SQL field names");
            var priceColumn = grid.Columns.Single(c => c.Header.ToString() == "Цена");
            var priceText = ((TextBlock)priceColumn.GetCellContent(grid.Items[0])).Text;
            Check(decimal.TryParse(priceText, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.GetCultureInfo("ru-RU"), out var visiblePrice)
                && visiblePrice == 1642.5m, "order visibly renders its stored unit price");
            var draft = new OrderDraft { UserId = 3, ChooseUser = true };
            Call(window, "ShowOrder", draft);
            Check(Control<ComboBox>(window, "Customer").IsEnabled && Control<DatePicker>(window, "OrderDate").IsEnabled, "new staff order selects customer and date");
            Snapshot(window, "new-order");
            draft.Items.Add(Item());
            Call(window, "RefreshItems");
            Control<DataGrid>(window, "Items").SelectedIndex = 0;
            Control<TextBox>(window, "LineQuantity").Text = "3";
            Call(window, "ChangeQuantity", null, null);
            Check(draft.Items[0].Count == 3 && draft.Items[0].Price == 1642.5m,
                "draft quantity can change without repricing");
            draft.Items.Clear();
            Call(window, "RefreshItems");
            Call(window, "ChooseItem", null, null);
            Check(Control<TabControl>(window, "Pages").SelectedIndex == 0 && app.Windows.Count == 1,
                "new order reuses catalog without another window");
            Control<ListBox>(window, "Products").SelectedIndex = 0;
            var selected = (CartItem)Call(window, "SelectedItem", true)!;
            var pick = (Action<CartItem>)typeof(MainWindow).GetField("pick", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
            pick(selected);
            Check(draft.Items.Single().Price == selected.Price,
                "catalog selection adds quoted item to new-order draft");
            TestGridFilters(window);
            window.Close();
            var guestLogin = new LoginWindow();
            Check(Elements(guestLogin).OfType<Control>().Where(c => c is Button or TextBox).All(c => c.ToolTip != null),
                "authorization controls have hover descriptions");
            typeof(LoginWindow).GetProperty("UserFIO")!.SetValue(guestLogin, "Гость");
            var guest = new MainWindow();
            guest.StartSession(guestLogin);
            Check(Control<Button>(guest, "CartButton").Visibility == Visibility.Collapsed
                && Control<Button>(guest, "OrdersButton").Visibility == Visibility.Collapsed,
                "guest has no cart or order-management navigation");
            Control<ListBox>(guest, "Products").SelectedIndex = 0;
            Check(Control<TabControl>(guest, "Pages").SelectedIndex == 1
                && Control<Button>(guest, "AddButton").Visibility == Visibility.Collapsed,
                "guest can view product but cannot order");
            var beforeOrders = Scalar("SELECT COUNT(*) FROM Orders_import");
            foreach (var handler in new[] { "AddItem", "OpenCart", "ChooseItem", "SaveOrder", "RemoveItem", "ChangeQuantity" })
                Call(guest, handler, null, null);
            Check(Control<TabControl>(guest, "Pages").SelectedIndex == 1
                && Scalar("SELECT COUNT(*) FROM Orders_import") == beforeOrders,
                "guest order handlers do not change orders or navigate to checkout");
            guest.Close();
            guestLogin.Close();
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

    private static void TestGridFilters(MainWindow window)
    {
        foreach (var name in new[] { "Orders", "Items" })
        {
            var grid = Control<DataGrid>(window, name);
            Call(window, "Page", name == "Orders" ? 2 : 3, "Проверка фильтров");
            var table = new DataTable();
            table.Columns.Add("Наименование", typeof(string));
            table.Columns.Add("Количество", typeof(int));
            table.Columns.Add("Дата_заказа", typeof(DateTime));
            table.Columns.Add("Цена", typeof(decimal));
            table.Rows.Add("Обувь O'Brien [100%]*", 2, new DateTime(2026, 11, 12), 1000m);
            table.Rows.Add("Обувь обычная", 3, new DateTime(2026, 11, 13), 2190m);
            table.Rows.Add("Другая модель", 2, new DateTime(2026, 11, 12), 1000.5m);
            grid.ItemsSource = table.DefaultView;
            grid.Measure(new Size(1080, 720));
            grid.Arrange(new Rect(0, 0, 1080, 720));
            grid.UpdateLayout();
            Snapshot(window, "filters-" + name);
            TextBox FilterBox(string column) => VisualElements(grid).OfType<TextBox>()
                .Single(t => t.Tag is ValueTuple<DataGrid, string> target && target.Item2 == column);
            var productFilter = FilterBox("Наименование");
            var quantityFilter = FilterBox("Количество");
            var dateFilter = FilterBox("Дата_заказа");
            productFilter.Text = "обувь";
            Check(grid.Items.Count == 2, name + $" header filters text case-insensitively (rows={grid.Items.Count}, filter={table.DefaultView.RowFilter})");
            quantityFilter.Text = "2";
            Check(grid.Items.Count == 1 && ((DataRowView)grid.Items[0])["Наименование"].ToString()!.Contains("O'Brien"),
                name + " column filters combine with AND");
            productFilter.Text = "O'Brien [100%]*";
            Check(grid.Items.Count == 1, name + " filter treats quotes and wildcard characters literally");
            productFilter.Text = "";
            quantityFilter.Text = "";
            dateFilter.Text = "12.11.2026";
            Check(grid.Items.Count == 2, name + " date filter matches displayed Russian date");
            table.DefaultView.Sort = "Количество DESC";
            dateFilter.Text = "";
            Check(grid.Items.Count == 3 && Convert.ToInt32(((DataRowView)grid.Items[0])["Количество"]) == 3,
                name + " clearing filters restores rows and preserves sorting");
            var priceFilter = FilterBox("Цена");
            priceFilter.Text = "1\u00a0000,00";
            Check(grid.Items.Count == 1 && Convert.ToDecimal(((DataRowView)grid.Items[0])["Цена"]) == 1000m,
                name + " numeric filter accepts the displayed price with grouping and trailing zeroes");
            priceFilter.Text = "";
        }
    }

    private static IEnumerable<DependencyObject> VisualElements(DependencyObject root)
    {
        yield return root;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in VisualElements(VisualTreeHelper.GetChild(root, i))) yield return child;
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

    private static IEnumerable<DependencyObject> Elements(DependencyObject root)
    {
        yield return root;
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            foreach (var element in Elements(child)) yield return element;
    }
}
