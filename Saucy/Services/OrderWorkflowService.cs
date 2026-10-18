using SaucyMVC.Data;
using SaucyMVC.Models;

namespace SaucyMVC.Services;

public class OrderWorkflowService
{
    private readonly SaucyDbContext _db;

    public OrderWorkflowService(SaucyDbContext db) => _db = db;

    public static string? GetStaffNextStatus(string currentStatus) => currentStatus switch
    {
        "Confirmed" => "Preparing",
        "Preparing" => "Ready",
        "Ready" => "Completed",
        _ => null
    };

    public void RecordInitial(Order order, int? actorUserId, string? actorName, string? actorRole)
    {
        AddHistory(order, "", order.Status, "Order created", actorUserId, actorName, actorRole, "Order submitted by customer.");
    }

    public bool ChangeStatus(Order order, string newStatus, int? actorUserId, string? actorName, string? actorRole, string? note = null)
    {
        if (order.Status == newStatus) return false;
        var previousStatus = order.Status;
        order.Status = newStatus;
        AddHistory(order, previousStatus, newStatus, "Status changed", actorUserId, actorName, actorRole, note);
        return true;
    }

    public void RecordEvent(Order order, string action, int? actorUserId, string? actorName, string? actorRole, string? note = null)
    {
        AddHistory(order, order.Status, order.Status, action, actorUserId, actorName, actorRole, note);
    }

    private void AddHistory(Order order, string fromStatus, string toStatus, string action, int? actorUserId, string? actorName, string? actorRole, string? note)
    {
        _db.OrderStatusHistory.Add(new OrderStatusHistory
        {
            Order = order,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Action = action,
            ChangedByUserId = actorUserId,
            ChangedByName = string.IsNullOrWhiteSpace(actorName) ? "System" : actorName,
            ChangedByRole = string.IsNullOrWhiteSpace(actorRole) ? "System" : actorRole,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        });
    }
}
