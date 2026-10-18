using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaucyMVC.Data;
using SaucyMVC.Models;
using SaucyMVC.Services;

namespace SaucyMVC.Controllers;

public class CartController : BaseController
{
    private readonly CartService _cart;
    private readonly SaucyDbContext _db;
    private readonly OrderWorkflowService _workflow;

    public CartController(CartService cart, SaucyDbContext db, OrderWorkflowService workflow)
    {
        _cart = cart;
        _db = db;
        _workflow = workflow;
    }

    public async Task<IActionResult> Index()
    {
        if (!CurrentUserId.HasValue)
            return RedirectToAction("Login", "Account", new { returnUrl = "/Cart" });

        var earliest = DateTime.Now.AddHours(-6);
        ViewBag.ConfirmedReservations = await _db.Reservations
            .Include(x => x.DiningTable)
            .Where(x => x.UserId == CurrentUserId
                && x.Status == "Confirmed"
                && x.ReservationDate >= earliest
                && !_db.Orders.Any(order => order.ReservationId == x.Id && order.Status != "Cancelled"))
            .OrderBy(x => x.ReservationDate)
            .ToListAsync();
        ViewBag.PendingReservations = await _db.Reservations
            .Include(x => x.DiningTable)
            .Where(x => x.UserId == CurrentUserId
                && x.Status == "Pending"
                && x.ReservationDate >= earliest)
            .OrderBy(x => x.ReservationDate)
            .ToListAsync();

        return View(_cart.Get());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, int quantity = 1, int? sauce1Id = null, int? sauce2Id = null, string? returnUrl = null)
    {
        if (!CurrentUserId.HasValue)
        {
            TempData["Error"] = "Please sign in to add items to your cart.";
            return RedirectToAction("Login", "Account", new
            {
                returnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Menu" : returnUrl
            });
        }

        var result = await _cart.AddAsync(id, quantity, sauce1Id, sauce2Id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return !string.IsNullOrWhiteSpace(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Menu");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Update(int id, int quantity, int? sauce1Id = null, int? sauce2Id = null)
    {
        if (!CurrentUserId.HasValue) return LoginRequired();
        _cart.Update(id, sauce1Id, sauce2Id, quantity);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Remove(int id, int? sauce1Id = null, int? sauce2Id = null)
    {
        if (!CurrentUserId.HasValue) return LoginRequired();
        _cart.Remove(id, sauce1Id, sauce2Id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string orderType = "Dine-in", int? reservationId = null, string? notes = null)
    {
        if (!CurrentUserId.HasValue) return LoginRequired();

        var lines = _cart.Get();
        if (lines.Count == 0)
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction(nameof(Index));
        }

        var validation = await _cart.ValidateAsync();
        if (!validation.Success)
        {
            TempData["Error"] = validation.Message;
            return RedirectToAction(nameof(Index));
        }

        orderType = orderType == "Takeout" ? "Takeout" : "Dine-in";
        if (orderType == "Dine-in")
        {
            if (reservationId.HasValue)
            {
                var reservation = await _db.Reservations
                    .Include(x => x.DiningTable)
                    .FirstOrDefaultAsync(x => x.Id == reservationId
                        && x.UserId == CurrentUserId
                        && x.Status == "Confirmed"
                        && x.DiningTableId != null);

                if (reservation == null)
                {
                    TempData["Error"] = "That reservation is not confirmed or no longer available.";
                    return RedirectToAction(nameof(Index));
                }

                if (await _db.Orders.AnyAsync(x => x.ReservationId == reservation.Id && x.Status != "Cancelled"))
                {
                    TempData["Error"] = "An order is already linked to that reservation.";
                    return RedirectToAction(nameof(Index));
                }

                var dineInOrder = BuildOrder(lines, "Dine-in", notes, reservation);
                _db.Orders.Add(dineInOrder);
                _db.Payments.Add(BuildPayment(dineInOrder, reservation));
                _workflow.RecordInitial(dineInOrder, CurrentUserId, CurrentUserName, CurrentRole);
                await _db.SaveChangesAsync();
                _cart.Clear();
                HttpContext.Session.Remove(CheckoutSession.DineInNotesKey);

                return RedirectToAction("Pay", "Payments", new { id = dineInOrder.Id });
            }

            if (string.IsNullOrWhiteSpace(notes))
                HttpContext.Session.Remove(CheckoutSession.DineInNotesKey);
            else
                HttpContext.Session.SetString(CheckoutSession.DineInNotesKey, notes.Trim());

            return RedirectToAction("Create", "Reservations", new { checkout = true });
        }

        var order = BuildOrder(lines, "Takeout", notes);
        var payment = BuildPayment(order);

        _db.Orders.Add(order);
        _db.Payments.Add(payment);
        _workflow.RecordInitial(order, CurrentUserId, CurrentUserName, CurrentRole);
        await _db.SaveChangesAsync();
        _cart.Clear();

        return RedirectToAction("Pay", "Payments", new { id = order.Id });
    }

    private Order BuildOrder(IReadOnlyCollection<CartLine> lines, string orderType, string? notes, Reservation? reservation = null) => new()
    {
        UserId = CurrentUserId!.Value,
        Reservation = reservation,
        OrderType = orderType,
        Notes = notes,
        Status = "Pending",
        TotalAmount = lines.Sum(x => x.LineTotal),
        Items = lines.Select(x => new OrderItem
        {
            MenuItemId = x.MenuItemId,
            Quantity = x.Quantity,
            UnitPrice = x.Price,
            Sauce1Name = x.Sauce1,
            Sauce2Name = x.Sauce2
        }).ToList()
    };

    private Payment BuildPayment(Order order, Reservation? reservation = null) => new()
    {
        UserId = CurrentUserId!.Value,
        Order = order,
        Reservation = reservation,
        Amount = order.TotalAmount,
        Status = "Pending"
    };
}
