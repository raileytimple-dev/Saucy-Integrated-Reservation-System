using System.ComponentModel.DataAnnotations;

namespace SaucyMVC.Models;

public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [MaxLength(25)] public string FromStatus { get; set; } = "";
    [MaxLength(25)] public string ToStatus { get; set; } = "";
    [MaxLength(50)] public string Action { get; set; } = "Status changed";
    public int? ChangedByUserId { get; set; }
    [MaxLength(120)] public string ChangedByName { get; set; } = "System";
    [MaxLength(20)] public string ChangedByRole { get; set; } = "System";
    public DateTime ChangedAt { get; set; } = DateTime.Now;
    [MaxLength(500)] public string? Note { get; set; }
}
