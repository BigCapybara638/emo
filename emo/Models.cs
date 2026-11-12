namespace emo;

public static class Pricing
{
    public static (DateTime Start, DateTime End) PreviousMonth(DateTime today)
    {
        var end = new DateTime(today.Year, today.Month, 1);
        return (end.AddMonths(-1), end);
    }

    public static decimal Apply(decimal price, bool discount) => discount ? Math.Round(price * 0.75m, 2) : price;
}

public class Product
{
    public int Id
    {
        get; set;
    }
    public string Image { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Subcategory { get; set; } = "";
    public string Manufacture { get; set; } = "";
    public string Composition { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal BasePrice
    {
        get; set;
    }
    public bool Discounted
    {
        get; set;
    }
    public decimal Price => Pricing.Apply(BasePrice, Discounted);
    public int TotalCount
    {
        get; set;
    }
    public string CountText => TotalCount > 5 ? "много" : "мало";
    public bool LowStock => TotalCount <= 3;
    public string ImagePath => ResourcePath(Image);
    public static string ResourcePath(string name)
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Resources", System.IO.Path.GetFileName(name));
        return System.IO.File.Exists(path) ? path : System.IO.Path.Combine(AppContext.BaseDirectory, "Resources", "picture.png");
    }
    public string PriceText => Discounted ? $"{Price:N2} ₽  •  скидка 25%" : $"{Price:N2} ₽";
}

public record ComboBoxItem(int Value, string Size, int MaxCount)
{
    public override string ToString() => $"{Size} (осталось {MaxCount})";
}

public class CartItem
{
    public int StockItemId
    {
        get; set;
    }
    public string ProductName { get; set; } = "";
    public string Manufacture { get; set; } = "";
    public string SizeName { get; set; } = "";
    public int Count
    {
        get; set;
    }
    public decimal Price
    {
        get; set;
    }
    public int Discount
    {
        get; set;
    }
    public decimal Total => Count * Price;
}

public class OrderDraft
{
    public int? Id
    {
        get; set;
    }
    public int UserId
    {
        get; set;
    }
    public DateTime Date { get; set; } = DateTime.Today;
    public List<CartItem> Items { get; set; } = [];
    public bool FromCart
    {
        get; set;
    }
    public bool ChooseUser
    {
        get; set;
    }

    public void ChangeQuantity(int index, int count, int available)
    {
        if (count <= 0)
            throw new InvalidOperationException("Введите целое положительное количество пар.");
        var item = Items[index];
        var others = Items.Where((i, position) => position != index && i.StockItemId == item.StockItemId).Sum(i => (long)i.Count);
        if (others + count > available)
            throw new InvalidOperationException($"Доступно {available} пар, в других позициях уже выбрано {others}. Уменьшите количество.");
        item.Count = count;
    }
}
