using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace emo;

public partial class MainWindow : Window
{
    private readonly Stack<Action> history = new();
    private readonly List<CartItem> cart = new();
    private Action show = () => { };
    private Action<CartItem>? pick;
    private IReadOnlyList<CartItem> reserved = [];
    private List<Product> products = [];
    private Product product = new();
    private OrderDraft order = new();
    private int userId, role;
    private bool Staff => role is 1 or 2;
    private bool Admin => role == 1;
    private bool Guest => userId == 0;

    public MainWindow()
    {
        InitializeComponent();
    }

    private static void Run(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка — проверьте данные и повторите действие", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private static bool Confirm(string text) => MessageBox.Show(text, "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    private static void Info(string text) => MessageBox.Show(text, "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

    internal void StartSession(LoginWindow login)
    {
        userId = login.UserId;
        role = login.UserRole;
        UserName.Text = login.UserFIO;
        cart.Clear();
        history.Clear();
        show = () => ShowCatalog();
        Run(show);
    }

    private void Go(Action next)
    {
        history.Push(show);
        show = next;
        Run(show);
        BackButton.IsEnabled = history.Count > 0;
    }

    private void BackClick(object sender, RoutedEventArgs e) => Back();
    private void Back()
    {
        if (history.Count == 0)
            return;
        if (Pages.SelectedIndex == 3 && order.Id == null && order.Items.Count > 0 && !order.FromCart
            && !Confirm("Отменить новый заказ? Несохранённые позиции будут потеряны."))
            return;
        show = history.Pop();
        Run(show);
        BackButton.IsEnabled = history.Count > 0;
    }

    private void Page(int index, string title)
    {
        Pages.SelectedIndex = index;
        PageTitle.Text = title;
        Title = $"Чудо Обувь — {title}";
        BackButton.IsEnabled = history.Count > 0;
    }

    private void ShowCatalog(Action<CartItem>? select = null, IReadOnlyList<CartItem>? pending = null)
    {
        pick = select;
        reserved = pending ?? cart;
        Page(0, select == null ? "Каталог" : "Выбор товара в заказ");
        OrdersButton.Visibility = Staff && select == null ? Visibility.Visible : Visibility.Collapsed;
        CartButton.Visibility = select == null && !Guest ? Visibility.Visible : Visibility.Collapsed;
        products = DBHelper.LoadProducts();
        var category = Category.SelectedItem as string;
        var categories = new[] { "Все категории" }.Concat(DBHelper.Categories()).ToArray();
        Category.ItemsSource = categories;
        Category.SelectedItem = category != null && categories.Contains(category) ? category : "Все категории";
        ApplyFilter();
    }

    private void Filter(object sender, RoutedEventArgs e) => ApplyFilter();
    private void ApplyFilter()
    {
        if (Products == null || Search == null || Category == null || Sort == null)
            return;
        var text = Search.Text.Trim();
        var filtered = products.Where(p => (Category.SelectedItem is not string c || c == "Все категории" || p.Category == c)
            && (p.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || p.Description.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || p.Composition.Contains(text, StringComparison.CurrentCultureIgnoreCase)));
        Products.ItemsSource = (Sort.SelectedIndex == 0 ? filtered.OrderBy(p => p.Price) : filtered.OrderByDescending(p => p.Price)).ToList();
    }

    private void SelectProduct(object sender, SelectionChangedEventArgs e)
    {
        if (Products.SelectedItem is not Product selected)
            return;
        var select = pick;
        var pending = reserved;
        Products.SelectedItem = null;
        Go(() => ShowProduct(selected.Id, select, pending));
    }

    private void ShowProduct(int id, Action<CartItem>? select, IReadOnlyList<CartItem> pending)
    {
        Page(1, "Карточка товара");
        pick = select;
        reserved = pending;
        product = DBHelper.LoadProducts(id).SingleOrDefault()
            ?? throw new InvalidOperationException("Товар уже удалён. Вернитесь кнопкой «Назад» и выберите другую модель.");
        ProductDetails.DataContext = product;
        var sizes = DBHelper.LoadSizes(id);
        SizeRange.Text = "Размерный ряд: " + string.Join(", ", sizes.Select(s => s.Size));
        SizeChoice.ItemsSource = sizes.Where(s => s.MaxCount > 0).ToArray();
        SizeChoice.SelectedIndex = 0;
        Quantity.Text = "1";
        AddButton.Content = select == null ? "В корзину" : "Добавить в заказ";
        AddButton.Visibility = Guest ? Visibility.Collapsed : Visibility.Visible;
        BuyButton.Visibility = select == null && !Guest ? Visibility.Visible : Visibility.Collapsed;
        ValidateProductQuantity(this, new RoutedEventArgs());
    }

    private CartItem SelectedItem(bool includePending)
    {
        if (SizeChoice.SelectedItem is not ComboBoxItem size || !int.TryParse(Quantity.Text, out int count) || count <= 0)
            throw new InvalidOperationException("Выберите доступный размер и введите целое положительное количество пар.");
        var pending = includePending ? reserved.Where(i => i.StockItemId == size.Value).Sum(i => i.Count) : 0;
        if ((long)count + pending > size.MaxCount)
            throw new InvalidOperationException($"На складе {size.MaxCount} пар, уже выбрано {pending}. Уменьшите количество или обновите каталог.");
        return new CartItem
        {
            StockItemId = size.Value,
            ProductName = product.Name,
            Manufacture = product.Manufacture,
            SizeName = size.Size,
            Count = count,
            Price = product.Price,
            Discount = product.Discounted ? 25 : 0
        };
    }

    private void AddItem(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (Guest)
            return;
        var item = SelectedItem(true);
        if (pick == null)
            cart.Add(item);
        else
            pick(item);
        Info("Товар добавлен. Можно выбрать ещё товар или вернуться кнопкой «Назад».");
        ShowProduct(product.Id, pick, reserved);
    });

    private void BuyNow(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (Guest)
            return;
        var draft = new OrderDraft { UserId = userId, Items = [SelectedItem(false)] };
        Go(() => ShowOrder(draft));
    });

    private void OpenCart(object sender, RoutedEventArgs e)
    {
        if (Guest)
            return;
        var draft = new OrderDraft { UserId = userId, Items = cart, FromCart = true };
        Go(() => ShowOrder(draft));
    }

    private void OpenOrders(object sender, RoutedEventArgs e)
    {
        if (Staff)
            Go(ShowOrders);
    }

    private void ShowOrders()
    {
        Page(2, "Заказы");
        Orders.ItemsSource = DBHelper.Query(@"SELECT o.id, o.[Дата_заказа], u.[Фамилия], u.[Имя], u.[Отчество]
            FROM Orders_import o JOIN Users u ON o.id_user=u.id ORDER BY o.[Дата_заказа] DESC").DefaultView;
    }

    private int SelectedOrderId() => Orders.SelectedItem is DataRowView row ? Convert.ToInt32(row["id"])
        : throw new InvalidOperationException("Выберите заказ в таблице.");

    private void NewOrder(object sender, RoutedEventArgs e)
    {
        if (!Staff)
            return;
        var draft = new OrderDraft { UserId = userId, ChooseUser = true };
        Go(() => ShowOrder(draft));
    }

    private void ViewOrder(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (!Staff)
            return;
        var draft = new OrderDraft { Id = SelectedOrderId() };
        Go(() => ShowOrder(draft));
    });

    private void DeleteOrder(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (!Staff)
            return;
        var id = SelectedOrderId();
        if (!Confirm("Удалить заказ? Все его товары вернутся на склад."))
            return;
        DBHelper.DeleteOrder(id);
        ShowOrders();
    });

    private void ShowOrder(OrderDraft draft)
    {
        order = draft;
        Page(3, draft.Id is { } id ? $"Состав заказа №{id}" : draft.FromCart ? "Корзина" : "Новый заказ");
        if (draft.Id is { } existing)
        {
            var header = DBHelper.Query("SELECT [Дата_заказа],id_user FROM Orders_import WHERE id=@id", ("@id", existing));
            if (header.Rows.Count == 0)
                throw new InvalidOperationException("Заказ уже удалён. Вернитесь кнопкой «Назад» к списку заказов.");
            var row = header.Rows[0];
            draft.Date = Convert.ToDateTime(row["Дата_заказа"]);
            draft.UserId = Convert.ToInt32(row["id_user"]);
        }
        OrderDate.SelectedDate = draft.Date;
        OrderDate.IsEnabled = draft.Id == null ? draft.ChooseUser && Staff : Admin;
        Customer.ItemsSource = DBHelper.Users().DefaultView;
        Customer.SelectedValue = draft.UserId;
        Customer.IsEnabled = draft.Id == null && draft.ChooseUser && Staff;
        SaveButton.Content = draft.Id == null ? "Подтвердить заказ" : "Сохранить дату";
        SaveButton.Visibility = draft.Id == null || Admin ? Visibility.Visible : Visibility.Collapsed;
        RemoveButton.Visibility = draft.Id == null || Admin ? Visibility.Visible : Visibility.Collapsed;
        QuantityEditor.Visibility = RemoveButton.Visibility;
        RefreshItems();
    }

    private void RefreshItems()
    {
        if (order.Id is { } id)
        {
            var table = DBHelper.OrderItems(id);
            Items.ItemsSource = table.DefaultView;
            Total.Text = $"Итого: {table.AsEnumerable().Sum(r => Convert.ToDecimal(r["Сумма"])):N2} ₽";
        }
        else
        {
            var table = new DataTable();
            foreach (var (name, type) in new[] { ("Index", typeof(int)), ("id_stock_items", typeof(int)), ("Наименование", typeof(string)),
                ("Производство", typeof(string)), ("Размер", typeof(string)), ("Количество", typeof(int)),
                ("Цена", typeof(decimal)), ("Скидка", typeof(int)), ("Сумма", typeof(decimal)) })
                table.Columns.Add(name, type);
            foreach (var (item, index) in order.Items.Select((item, index) => (item, index)))
            {
                table.Rows.Add(index, item.StockItemId, item.ProductName, item.Manufacture, item.SizeName, item.Count, item.Price, item.Discount, item.Total);
            }
            Items.ItemsSource = table.DefaultView;
            Total.Text = $"Итого: {order.Items.Sum(i => i.Total):N2} ₽";
        }
        RemoveButton.IsEnabled = ChangeQuantityButton.IsEnabled = Items.SelectedItem != null;
        SaveButton.IsEnabled = order.Id != null || order.Items.Count > 0;
    }

    private void ChooseItem(object sender, RoutedEventArgs e)
    {
        if (Guest)
            return;
        var draft = order;
        draft.Date = OrderDate.SelectedDate ?? draft.Date;
        if (Customer.SelectedValue is int selectedUser)
            draft.UserId = selectedUser;
        Go(() => ShowCatalog(item =>
        {
            if (draft.Id is { } id)
                DBHelper.SaveOrder(id, draft.UserId, draft.Date, [item]);
            else
                draft.Items.Add(item);
        }, draft.Id == null ? draft.Items : []));
    }

    private void RemoveItem(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (Guest)
            return;
        if (Items.SelectedIndex < 0)
            throw new InvalidOperationException("Выберите позицию в таблице.");
        if (order.Id is { } id)
        {
            if (!Admin || !Confirm("Удалить позицию из заказа и вернуть товар на склад?"))
                return;
            var row = (DataRowView)Items.SelectedItem;
            DBHelper.DeleteOrder(id, Convert.ToInt32(row["id_stock_items"]), Convert.ToDecimal(row["Цена"]), Convert.ToInt32(row["Скидка"]));
            if (DBHelper.OrderItems(id).Rows.Count == 0)
            {
                Back();
                return;
            }
        }
        else
            order.Items.RemoveAt(Convert.ToInt32(((DataRowView)Items.SelectedItem)["Index"]));
        RefreshItems();
    });

    private void SaveOrder(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (Guest)
            return;
        if (OrderDate.SelectedDate is not DateTime date || Customer.SelectedValue is not int customer)
            throw new InvalidOperationException("Выберите корректную дату и клиента из списка.");
        if (!Confirm(order.Id == null ? "Подтвердить оформление заказа?" : "Сохранить дату заказа?"))
            return;
        if (order.Id is { } id)
        {
            if (!Admin)
                return;
            DBHelper.Execute("UPDATE Orders_import SET [Дата_заказа]=@date WHERE id=@id", ("@date", date), ("@id", id));
            order.Date = date;
        }
        else
        {
            order.Id = DBHelper.SaveOrder(null, customer, date, order.Items);
            if (order.FromCart)
                cart.Clear();
            Info("Заказ оформлен.");
            Back();
        }
    });

    private void GenerateColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        e.Cancel = e.PropertyName is "id" or "id_stock_items" or "Index";
        e.Column.MinWidth = e.PropertyName == "Наименование" ? 240 : e.PropertyName == "Производство" ? 150 : 100;
        var style = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
        style.Setters.Add(new Setter(ToolTipProperty, e.PropertyName switch
        {
            "Наименование" => "Название модели обуви.",
            "Цена" => "Сохранённая цена за одну пару со скидкой, в рублях.",
            "Количество" => "Количество пар выбранной модели и размера.",
            "Размер" => "Размер обуви выбранной позиции.",
            "Скидка" => "Процент скидки при оформлении позиции.",
            "Сумма" => "Цена одной пары, умноженная на количество.",
            "Дата_заказа" => "Дата оформления заказа.",
            "Производство" => "Производитель модели обуви.",
            "Фамилия" => "Фамилия клиента.",
            "Имя" => "Имя клиента.",
            "Отчество" => "Отчество клиента.",
            _ => "Фамилия, имя и отчество клиента заказа."
        }));
        e.Column.HeaderStyle = style;
        if (e.Column is DataGridTextColumn column)
            column.Binding = new Binding(((DataGrid)sender).ItemsSource is DataView ? $"[{e.PropertyName}]" : e.PropertyName)
            {
                StringFormat = e.PropertyName == "Размер" ? "0.#" : e.PropertyType == typeof(decimal) ? "N2" : null,
                ConverterCulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU")
            };
    }

    private void Logout(object sender, RoutedEventArgs e)
    {
        if (!Confirm("Выйти из аккаунта? Несохранённые позиции будут потеряны."))
            return;
        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        Close();
        ((App)Application.Current).SignIn();
    }

    private void SelectLine(object sender, SelectionChangedEventArgs e)
    {
        if (Items.SelectedItem is DataRowView row)
            LineQuantity.Text = row["Количество"].ToString();
        RemoveButton.IsEnabled = ChangeQuantityButton.IsEnabled = Items.SelectedItem != null;
    }

    private void ChangeQuantity(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (Guest || (order.Id != null && !Admin))
            return;
        if (Items.SelectedItem is not DataRowView row || !int.TryParse(LineQuantity.Text, out var count) || count <= 0)
            throw new InvalidOperationException("Выберите позицию и введите целое положительное количество пар.");
        var stockId = Convert.ToInt32(row["id_stock_items"]);
        if (order.Id is { } id)
            DBHelper.ChangeQuantity(id, stockId, count);
        else
        {
            var available = Convert.ToInt32(DBHelper.Query("SELECT count FROM Stock_items WHERE id=@id", ("@id", stockId)).Rows[0][0]);
            order.ChangeQuantity(Convert.ToInt32(row["Index"]), count, available);
        }
        RefreshItems();
    });

    private void OnlyDigits(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsAsciiDigit);
    }

    private void ValidateProductQuantity(object sender, RoutedEventArgs e)
    {
        if (AddButton == null || BuyButton == null || QuantityHint == null || SizeChoice == null)
            return;
        var size = SizeChoice.SelectedItem as ComboBoxItem;
        var countValid = int.TryParse(Quantity.Text, out int count) && count > 0;
        var pending = size == null ? 0 : reserved.Where(i => i.StockItemId == size.Value).Sum(i => (long)i.Count);
        BuyButton.IsEnabled = countValid && size != null && count <= size.MaxCount;
        AddButton.IsEnabled = BuyButton.IsEnabled && count + pending <= size!.MaxCount;
        QuantityHint.Text = size == null ? "Нет доступных размеров. Выберите другую модель."
            : !countValid ? "Введите целое положительное количество пар."
            : $"Доступно {size.MaxCount} пар; уже выбрано в других позициях: {pending}.";
        QuantityHint.Foreground = AddButton.IsEnabled ? System.Windows.Media.Brushes.DimGray : System.Windows.Media.Brushes.DarkRed;
    }

    private void PasteDigits(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetData(DataFormats.UnicodeText) is not string text || text.Length == 0 || !text.All(char.IsAsciiDigit))
            e.CancelCommand();
    }
}
