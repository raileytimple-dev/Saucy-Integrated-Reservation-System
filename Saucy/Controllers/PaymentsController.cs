using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaucyMVC.Data;
using SaucyMVC.Models;
using SaucyMVC.Services;

namespace SaucyMVC.Controllers;

public class PaymentsController : BaseController
{
    private readonly SaucyDbContext _db;
    private readonly OrderWorkflowService _workflow;

    public PaymentsController(SaucyDbContext db, OrderWorkflowService workflow)
    {
        _db = db;
        _workflow = workflow;
    }

    [HttpGet]
    public async Task<IActionResult> Pay(int id)
    {
        if (!CurrentUserId.HasValue) return LoginRequired();

        var payment = await PaymentQuery()
            .FirstOrDefaultAsync(x => x.OrderId == id && x.UserId == CurrentUserId);

        if (payment == null) return NotFound();
        if (payment.Order?.OrderType == "Dine-in" && payment.Reservation?.Status != "Confirmed")
            return RedirectToAction("AwaitingConfirmation", "Reservations", new { id = payment.ReservationId });

        return View(payment);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int id, string method, string? referenceNumber)
    {
        if (!CurrentUserId.HasValue) return LoginRequired();

        var payment = await PaymentQuery()
            .FirstOrDefaultAsync(x => x.OrderId == id && x.UserId == CurrentUserId);
        if (payment == null) return NotFound();
        if (payment.Order?.OrderType == "Dine-in" && payment.Reservation?.Status != "Confirmed")
        {
            TempData["Error"] = "The table must be confirmed before payment.";
            return RedirectToAction("AwaitingConfirmation", "Reservations", new { id = payment.ReservationId });
        }

        if (payment.Status == "Paid")
        {
            TempData["Success"] = "This order is already paid.";
            return RedirectToAction("Track", "Orders");
        }

        method = method == "QR Pay" ? "QR Pay" : "Cash";
        if (method == "QR Pay" && string.IsNullOrWhiteSpace(referenceNumber))
        {
            ModelState.AddModelError("referenceNumber", "Enter the reference number shown after your QR payment.");
            payment.Method = method;
            return View(payment);
        }

        var normalizedReference = method == "QR Pay" ? referenceNumber!.Trim() : null;
        if (normalizedReference != null && await _db.Payments.AnyAsync(existing =>
            existing.Id != payment.Id
            && existing.Method == "QR Pay"
            && existing.Status == "Paid"
            && existing.ReferenceNumber == normalizedReference))
        {
            ModelState.AddModelError("referenceNumber", "That QR reference number has already been used.");
            payment.Method = method;
            payment.ReferenceNumber = normalizedReference;
            return View(payment);
        }

        payment.Method = method;
        payment.ReferenceNumber = normalizedReference;

        if (method == "Cash")
        {
            payment.Status = "Pending";
            if (payment.Order != null)
                _workflow.RecordEvent(payment.Order, "Cash payment selected", CurrentUserId, CurrentUserName, CurrentRole, "Waiting for counter payment verification.");
            await _db.SaveChangesAsync();
            TempData["Success"] = "Cash payment selected. Please pay at the counter when you arrive.";
            return RedirectToAction("Track", "Orders");
        }

        payment.Status = "Paid";
        if (payment.Order != null)
            _workflow.ChangeStatus(payment.Order, "Confirmed", CurrentUserId, CurrentUserName, CurrentRole, "QR reference accepted by automated checkout.");
        if (!await _db.TransactionHistory.AnyAsync(x => x.OrderId == id))
        {
            var itemText = payment.Order == null
                ? ""
                : string.Join(", ", payment.Order.Items.Select(item => $"{item.Quantity}x {item.MenuItem?.Name}{(item.Sauce1Name == null ? "" : $" [{item.Sauce1Name} + {item.Sauce2Name}]")}"));

            _db.TransactionHistory.Add(new TransactionHistory
            {
                UserId = CurrentUserId.Value,
                OrderId = id,
                TotalAmount = payment.Amount,
                OrderItems = itemText,
                PaymentMethod = method
            });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "QR payment recorded. Your order was automatically sent to the staff queue.";
        return RedirectToAction("Track", "Orders");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestRefund(int orderId, string description)
    {
        if (!CurrentUserId.HasValue) return LoginRequired();

        var order = await _db.Orders
            .FirstOrDefaultAsync(x => x.Id == orderId && x.UserId == CurrentUserId);
        if (order == null) return NotFound();

        if (!await _db.RefundRequests.AnyAsync(x => x.OrderId == orderId && x.Status == "Pending"))
        {
            _db.RefundRequests.Add(new RefundRequest
            {
                UserId = CurrentUserId.Value,
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Description = description
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Refund request submitted.";
        }

        return RedirectToAction("History", "Orders");
    }

    private IQueryable<Payment> PaymentQuery() => _db.Payments
        .Include(x => x.Reservation)
            .ThenInclude(reservation => reservation!.DiningTable)
        .Include(x => x.Order)
            .ThenInclude(order => order!.Items)
                .ThenInclude(item => item.MenuItem);
}
