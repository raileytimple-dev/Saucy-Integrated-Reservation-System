using System.ComponentModel.DataAnnotations;

namespace SaucyMVC.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
    public DateTime OrderedAt { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Pending";
    public decimal TotalAmount { get; set; }
    public string OrderType { get; set; } = "Dine-in";
    public string? Notes { get; set; }

    [MaxLength(25)] public string? CancellationRequestStatus { get; set; }
    [MaxLength(500)] public string? CancellationReason { get; set; }
    public DateTime? CancellationRequestedAt { get; set; }
    public int? CancellationRequestedByUserId { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
}
