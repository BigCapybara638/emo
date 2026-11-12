using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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

    public MainWindow()
    {
        InitializeComponent();
        ContentRendered += (_, _) => Authorize();
    }

    private static void Run(Action action)
    {
        try { action(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка — проверьте данные и повторите действие", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private static bool Confirm(string text) => MessageBox.Show(text, "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    private static void Info(string text) => MessageBox.Show(text, "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

    private void Authorize()
    {
        var login = new LoginWindow { Owner = this };
        if (login.ShowDialog() != true)
        {
            Close();
            return;
        }
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
        if (history.Count == 0) return;
        if (Pages.SelectedIndex == 3 && order.Id == null && order.Items.Count > 0 && !order.FromCart
            && !Confirm("Отменить новый заказ? Несохранённые позиции будут потеряны.")) return;
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
        CartButton.Visibility = select == null ? Visibility.Visible : Visibility.Collapsed;
        products = DBHelper.LoadProducts();
        var category = Category.SelectedItem as string;
        Category.ItemsSource = new[] { "Все категории" }.Concat(products.Select(p => p.Category).Distinct().Order());
        Category.SelectedItem = category != null && products.Any(p => p.Category == category) ? category : "Все категории";
        ApplyFilter();
    }

    private void Filter(object sender, RoutedEventArgs e) => ApplyFilter();
    private void ApplyFilter()
    {
        if (Products == null || Search == null || Category == null || Sort == null) return;
        var text = Search.Text.Trim();
        var filtered = products.Where(p => (Category.SelectedItem is not string c || c == "Все категории" || p.Category == c)
            && (p.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || p.Description.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || p.Composition.Contains(text, StringComparison.CurrentCultureIgnoreCase)));
        Products.ItemsSource = (Sort.SelectedIndex == 0 ? filtered.OrderBy(p => p.Price) : filtered.OrderByDescending(p => p.Price)).ToList();
    }

    private void SelectProduct(object sender, SelectionChangedEventArgs e)
    {
        if (Products.SelectedItem is not Product selected) return;
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
        product = DBHelper.LoadProducts(id).Single();
        ProductDetails.DataContext = product;
        var sizes = DBHelper.LoadSizes(id);
        SizeRange.Text = "Размерный ряд: " + string.Join(", ", sizes.Select(s => s.Size));
        SizeChoice.ItemsSource = sizes.Where(s => s.MaxCount > 0).ToArray();
        SizeChoice.SelectedIndex = 0;
        Quantity.Text = "1";
        AddButton.Content = select == null ? "В корзину" : "Добавить в заказ";
        BuyButton.Visibility = select == null ? Visibility.Visible : Visibility.Collapsed;
        AddButton.IsEnabled = BuyButton.IsEnabled = SizeChoice.Items.Count > 0;
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
            StockItemId = size.Value, ProductName = $"{product.Manufacture} | {product.Name}", SizeName = size.Size,
            Count = count, Price = product.Price, Discount = product.Discounted ? 25 : 0
        };
    }

    private void AddItem(object sender, RoutedEventArgs e) => Run(() =>
    {
        var item = SelectedItem(true);
        if (pick == null) cart.Add(item);
        else pick(item);
        Info("Товар добавлен. Можно выбрать ещё товар или вернуться кнопкой «Назад».");
        ShowProduct(product.Id, pick, reserved);
    });

    private void BuyNow(object sender, RoutedEventArgs e) => Run(() =>
    {
        var draft = new OrderDraft { UserId = userId, Items = [SelectedItem(false)] };
        Go(() => ShowOrder(draft));
    });

    private void OpenCart(object sender, RoutedEventArgs e)
    {
        var draft = new OrderDraft { UserId = userId, Items = cart, FromCart = true };
        Go(() => ShowOrder(draft));
    }

    private void OpenOrders(object sender, RoutedEventArgs e)
    {
        if (Staff) Go(ShowOrders);
    }

    private void ShowOrders()
    {
        Page(2, "Заказы");
        Orders.ItemsSource = DBHelper.Query(@"SELECT o.id, o.[Дата заказа], CONCAT(u.[Фамилия],' ',u.[Имя],' ',u.[Отчество]) AS [ФИО клиента]
            FROM Orders_import$ o JOIN Users$ u ON o.id_user=u.id ORDER BY o.[Дата заказа] DESC").DefaultView;
    }

    private int SelectedOrderId() => Orders.SelectedItem is DataRowView row ? Convert.ToInt32(row["id"])
        : throw new InvalidOperationException("Выберите заказ в таблице.");

    private void NewOrder(object sender, RoutedEventArgs e)
    {
        if (!Staff) return;
        var draft = new OrderDraft { UserId = userId, ChooseUser = true };
        Go(() => ShowOrder(draft));
    }

    private void ViewOrder(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (!Staff) return;
        var draft = new OrderDraft { Id = SelectedOrderId() };
        Go(() => ShowOrder(draft));
    });

    private void DeleteOrder(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (!Staff) return;
        var id = SelectedOrderId();
        if (!Confirm("Удалить заказ? Все его товары вернутся на склад.")) return;
        DBHelper.DeleteOrder(id);
        ShowOrders();
    });

    private void ShowOrder(OrderDraft draft)
    {
        order = draft;
        Page(3, draft.Id is { } id ? $"Состав заказа №{id}" : draft.FromCart ? "Корзина" : "Новый заказ");
        if (draft.Id is { } existing)
        {
            var row = DBHelper.Query("SELECT [Дата заказа],id_user FROM Orders_import$ WHERE id=@id", ("@id", existing)).Rows[0];
            draft.Date = Convert.ToDateTime(row["Дата заказа"]);
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
            Items.ItemsSource = order.Items.Select((i, index) => new
            {
                Index = index, Товар = i.ProductName, Размер = i.SizeName, Количество = i.Count,
                Цена = i.Price, Скидка = i.Discount, Сумма = i.Total
            }).ToList();
            Total.Text = $"Итого: {order.Items.Sum(i => i.Total):N2} ₽";
        }
    }

    private void ChooseItem(object sender, RoutedEventArgs e)
    {
        var draft = order;
        draft.Date = OrderDate.SelectedDate ?? draft.Date;
        if (Customer.SelectedValue is int selectedUser) draft.UserId = selectedUser;
        Go(() => ShowCatalog(item =>
        {
            if (draft.Id is { } id) DBHelper.SaveOrder(id, draft.UserId, draft.Date, [item]);
            else draft.Items.Add(item);
        }, draft.Id == null ? draft.Items : []));
    }

    private void RemoveItem(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (Items.SelectedIndex < 0) throw new InvalidOperationException("Выберите позицию в таблице.");
        if (order.Id is { } id)
        {
            if (!Admin || !Confirm("Удалить позицию из заказа и вернуть товар на склад?")) return;
            var row = (DataRowView)Items.SelectedItem;
            DBHelper.DeleteOrder(id, Convert.ToInt32(row["StockItemId"]), Convert.ToDecimal(row["Цена за ед."]), Convert.ToInt32(row["Скидка, %"]));
            if (DBHelper.OrderItems(id).Rows.Count == 0)
            {
                Back();
                return;
            }
        }
        else order.Items.RemoveAt((int)Items.SelectedItem.GetType().GetProperty("Index")!.GetValue(Items.SelectedItem)!);
        RefreshItems();
    });

    private void SaveOrder(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (OrderDate.SelectedDate is not DateTime date || Customer.SelectedValue is not int customer)
            throw new InvalidOperationException("Выберите корректную дату и клиента из списка.");
        if (!Confirm(order.Id == null ? "Подтвердить оформление заказа?" : "Сохранить дату заказа?")) return;
        if (order.Id is { } id)
        {
            if (!Admin) return;
            DBHelper.Execute("UPDATE Orders_import$ SET [Дата заказа]=@date WHERE id=@id", ("@date", date), ("@id", id));
            order.Date = date;
        }
        else
        {
            order.Id = DBHelper.SaveOrder(null, customer, date, order.Items);
            if (order.FromCart) cart.Clear();
            Info("Заказ оформлен.");
            Back();
        }
    });

    private void GenerateColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        e.Cancel = e.PropertyName is "id" or "StockItemId" or "Index";
        e.Column.MinWidth = e.PropertyName is "Товар" or "ФИО клиента" ? 240 : 100;
        if (e.Column is DataGridTextColumn column)
            column.Binding = new Binding(((DataGrid)sender).ItemsSource is DataView ? $"[{e.PropertyName}]" : e.PropertyName)
                { StringFormat = e.PropertyType == typeof(decimal) ? "N2" : null, ConverterCulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU") };
    }

    private void Logout(object sender, RoutedEventArgs e)
    {
        if (Confirm("Выйти из аккаунта? Несохранённые позиции будут потеряны.")) Authorize();
    }
}
