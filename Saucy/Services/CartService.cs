using Microsoft.EntityFrameworkCore;
using SaucyMVC.Data;

namespace SaucyMVC.Services;

public record CartLine(
    int MenuItemId,
    string Name,
    decimal Price,
    int Quantity,
    string? ImagePath,
    int? Sauce1Id = null,
    string? Sauce1 = null,
    int? Sauce2Id = null,
    string? Sauce2 = null)
{
    public decimal LineTotal => Price * Quantity;
    public bool HasSauces => Sauce1Id.HasValue && Sauce2Id.HasValue;
}

public record CartOperationResult(bool Success, string Message)
{
    public static CartOperationResult Ok(string message) => new(true, message);
    public static CartOperationResult Fail(string message) => new(false, message);
}

public class CartService
{
    private const string Key = "SaucyCart";
    private const string FamousOfferings = "Famous Offerings";
    private readonly IHttpContextAccessor _http;
    private readonly SaucyDbContext _db;

    public CartService(IHttpContextAccessor http, SaucyDbContext db)
    {
        _http = http;
        _db = db;
    }

    private ISession Session => _http.HttpContext!.Session;
    public List<CartLine> Get() => Session.GetObject<List<CartLine>>(Key) ?? [];
    public int Count => Get().Sum(line => line.Quantity);
    public decimal Total => Get().Sum(line => line.LineTotal);

    public async Task<CartOperationResult> AddAsync(int id, int quantity = 1, int? sauce1Id = null, int? sauce2Id = null)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item == null || !item.IsAvailable)
            return CartOperationResult.Fail("That menu item is currently unavailable.");

        string? sauce1Name = null;
        string? sauce2Name = null;
        if (item.Category == FamousOfferings)
        {
            if (!sauce1Id.HasValue || !sauce2Id.HasValue)
                return CartOperationResult.Fail("Choose two sauces for every Famous Offering meal.");

            var selectedIds = new[] { sauce1Id.Value, sauce2Id.Value }.Distinct().ToList();
            var sauces = await _db.SauceOptions
                .Where(sauce => selectedIds.Contains(sauce.Id) && sauce.IsAvailable)
                .ToDictionaryAsync(sauce => sauce.Id);

            if (!sauces.TryGetValue(sauce1Id.Value, out var first) || !sauces.TryGetValue(sauce2Id.Value, out var second))
                return CartOperationResult.Fail("One of your selected sauces is unavailable. Please choose another sauce.");

            // A + B is the same combination as B + A; sorting keeps the cart tidy.
            if (first.Id <= second.Id)
            {
                sauce1Id = first.Id;
                sauce1Name = first.Name;
                sauce2Id = second.Id;
                sauce2Name = second.Name;
            }
            else
            {
                sauce1Id = second.Id;
                sauce1Name = second.Name;
                sauce2Id = first.Id;
                sauce2Name = first.Name;
            }
        }
        else
        {
            sauce1Id = null;
            sauce2Id = null;
        }

        var list = Get();
        var existing = list.FirstOrDefault(line =>
            line.MenuItemId == id && line.Sauce1Id == sauce1Id && line.Sauce2Id == sauce2Id);
        var safeQuantity = Math.Max(1, quantity);
        if (existing != null)
        {
            list.Remove(existing);
            list.Add(existing with { Quantity = existing.Quantity + safeQuantity });
        }
        else
        {
            list.Add(new CartLine(id, item.Name, item.Price, safeQuantity, item.ImagePath,
                sauce1Id, sauce1Name, sauce2Id, sauce2Name));
        }

        Session.SetObject(Key, list);
        return CartOperationResult.Ok(item.Category == FamousOfferings
            ? $"{item.Name} with two sauces added to your cart."
            : $"{item.Name} added to your cart.");
    }

    public void Update(int id, int? sauce1Id, int? sauce2Id, int quantity)
    {
        var list = Get();
        var existing = FindLine(list, id, sauce1Id, sauce2Id);
        if (existing == null) return;
        list.Remove(existing);
        if (quantity > 0) list.Add(existing with { Quantity = quantity });
        Session.SetObject(Key, list);
    }

    public void Remove(int id, int? sauce1Id, int? sauce2Id)
    {
        var list = Get();
        list.RemoveAll(line => line.MenuItemId == id && line.Sauce1Id == sauce1Id && line.Sauce2Id == sauce2Id);
        Session.SetObject(Key, list);
    }

    public async Task<CartOperationResult> ValidateAsync()
    {
        var lines = Get();
        if (lines.Count == 0) return CartOperationResult.Fail("Your cart is empty.");

        var itemIds = lines.Select(line => line.MenuItemId).Distinct().ToList();
        var items = await _db.MenuItems
            .Where(item => itemIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id);
        var sauceIds = lines
            .SelectMany(line => new[] { line.Sauce1Id, line.Sauce2Id })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var availableSauceIds = await _db.SauceOptions
            .Where(sauce => sauceIds.Contains(sauce.Id) && sauce.IsAvailable)
            .Select(sauce => sauce.Id)
            .ToListAsync();

        foreach (var line in lines)
        {
            if (!items.TryGetValue(line.MenuItemId, out var item) || !item.IsAvailable)
                return CartOperationResult.Fail($"{line.Name} is no longer available. Remove it before checkout.");

            if (item.Category == FamousOfferings &&
                (!line.Sauce1Id.HasValue || !line.Sauce2Id.HasValue ||
                 !availableSauceIds.Contains(line.Sauce1Id.Value) ||
                 !availableSauceIds.Contains(line.Sauce2Id.Value)))
                return CartOperationResult.Fail($"Choose two currently available sauces for {line.Name} before checkout. Remove it and add it again from the menu.");
        }

        return CartOperationResult.Ok("Cart is ready.");
    }

    public void Clear() => Session.Remove(Key);

    private static CartLine? FindLine(List<CartLine> lines, int id, int? sauce1Id, int? sauce2Id) =>
        lines.FirstOrDefault(line => line.MenuItemId == id && line.Sauce1Id == sauce1Id && line.Sauce2Id == sauce2Id);
}
