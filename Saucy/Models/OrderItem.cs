using System.ComponentModel.DataAnnotations;

namespace SaucyMVC.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    [MaxLength(80)] public string? Sauce1Name { get; set; }
    [MaxLength(80)] public string? Sauce2Name { get; set; }
}
