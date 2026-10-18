using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaucyMVC.Data;
using SaucyMVC.Models;
using SaucyMVC.Services;
using SaucyMVC.ViewModels;

namespace SaucyMVC.Controllers;

public class ReservationsController : BaseController
{
    private static readonly string[] BlockingStatuses = ["Pending", "Accepted", "Confirmed", "Seated"];
    private readonly SaucyDbContext _db;
    private readonly CartService _cart;
    private readonly ReservationAutomationService _automation;

    public ReservationsController(
        SaucyDbContext db,
        CartService cart,
        ReservationAutomationService automation)
    {
        _db = db;
        _cart = cart;
        _automation = automation;
    }

    public async Task<IActionResult> Index()
    {
        if (!CurrentUserId.HasValue) return LoginRequired();
        await _automation.ProcessAsync();

        var reservations = await _db.Reservations
            .Include(x => x.DiningTable)
            .Where(x => x.UserId == CurrentUserId)
            .OrderByDescending(x => x.ReservationDate)
            .ToListAsync();

        ViewBag.CheckoutReservationId = HttpContext.Session.GetInt32(CheckoutSession.CheckoutReservationIdKey);
        ViewBag.HasCheckoutCart = _cart.Get().Count > 0;

        return View(reservations);
    }

    [HttpGet]
    public async Task<IActionResult> Create(bool checkout = false)
    {
        if (!CurrentUserId.HasValue)
        {
            ViewBag.IsGuestPreview = true;
            ViewBag.IsCheckoutFlow = checkout;
            return View(new ReservationViewModel());
        }

        if (checkout && _cart.Get().Count == 0)
        {
            TempData["Error"] = "Your cart is empty. Add your food before choosing a dine-in table.";
            return RedirectToAction("Index", "Cart");
        }

        await _automation.ProcessAsync();

        var model = new ReservationViewModel { ContinueToCheckout = checkout };
        await LoadTableAvailabilityAsync(model, model.Date.Date + model.Time);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Availability(DateTime? date, TimeSpan? time, int guestCount = 2)
    {
        if (!date.HasValue || !time.HasValue || guestCount is < 1 or > 30)
            return BadRequest(new { message = "Choose a valid date, time, and party size." });

        await _automation.ProcessAsync();

        var model = new ReservationViewModel
        {
            Date = date.Value.Date,
            Time = time.Value,
            GuestCount = guestCount
        };

        await LoadTableAvailabilityAsync(model, model.Date + model.Time);
        return Json(model.Tables.Select(table => new
        {
            table.Id,
            table.TableNumber,
            table.Capacity,
            table.State,
            table.IsAvailable
        }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationViewModel model)
    {
        if (!CurrentUserId.HasValue) return LoginRequired();
        await _automation.ProcessAsync();

        var reservationTime = model.Date.Date + model.Time;
        if (reservationTime <= DateTime.Now)
            ModelState.AddModelError(nameof(model.Date), "Reservation must be scheduled in the future.");

        var cartLines = model.ContinueToCheckout ? _cart.Get() : [];
        if (model.ContinueToCheckout && cartLines.Count == 0)
            ModelState.AddModelError("", "Your cart expired or is empty. Return to the menu and add your food again.");

        DiningTable? selectedTable = null;
        if (model.SelectedTableId.HasValue)
        {
            selectedTable = await _db.DiningTables
                .FirstOrDefaultAsync(table => table.Id == model.SelectedTableId && table.IsActive);

            if (selectedTable == null)
                ModelState.AddModelError(nameof(model.SelectedTableId), "That table is no longer active.");
            else if (selectedTable.Capacity < model.GuestCount)
                ModelState.AddModelError(nameof(model.SelectedTableId), $"{selectedTable.TableNumber} only seats {selectedTable.Capacity} people.");
            else if (IsOperationallyUnavailable(selectedTable, reservationTime))
                ModelState.AddModelError(nameof(model.SelectedTableId), $"{selectedTable.TableNumber} is occupied too close to that schedule. Please choose another table.");
            else if (await HasConflictAsync(selectedTable.Id, reservationTime))
                ModelState.AddModelError(nameof(model.SelectedTableId), $"{selectedTable.TableNumber} was just reserved. Please choose another table.");
        }

        if (!ModelState.IsValid)
        {
            await LoadTableAvailabilityAsync(model, reservationTime);
            return View(model);
        }

        var reservation = new Reservation
        {
            UserId = CurrentUserId.Value,
            DiningTableId = selectedTable!.Id,
            ReservationDate = reservationTime,
            GuestCount = model.GuestCount,
            Notes = model.Notes
        };
        _automation.BeginHold(reservation);

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        if (model.ContinueToCheckout)
        {
            HttpContext.Session.SetInt32(CheckoutSession.CheckoutReservationIdKey, reservation.Id);
            return RedirectToAction(nameof(AwaitingConfirmation), new { id = reservation.Id });
        }

        TempData["Success"] = $"{selectedTable.TableNumber} is held for {_automation.PendingHoldMinutes} minutes while Admin reviews your request.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> AwaitingConfirmation(int id)
    {
        if (!CurrentUserId.HasValue) return LoginRequired();
        await _automation.ProcessAsync();

        var reservation = await _db.Reservations
            .Include(x => x.DiningTable)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId);
        if (reservation == null) return NotFound();

        if (reservation.Status == "Confirmed")
            ViewBag.CanContinue = _cart.Get().Count > 0;
        ViewBag.HoldMinutes = _automation.PendingHoldMinutes;

        return View(reservation);
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmationStatus(int id)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();
        await _automation.ProcessAsync();

        var reservation = await _db.Reservations
            .Where(x => x.Id == id && x.UserId == CurrentUserId)
            .Select(x => new { x.Status, x.HoldExpiresAt })
            .FirstOrDefaultAsync();

        return reservation == null
            ? NotFound()
            : Json(new
            {
                reservation.Status,
                reservation.HoldExpiresAt,
                canContinue = reservation.Status == "Confirmed" && _cart.Get().Count > 0
            });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> FinalizeCheckout(int id)
    {
        if (!CurrentUserId.HasValue) return LoginRequired();
        await _automation.ProcessAsync();

        var reservation = await _db.Reservations
            .Include(x => x.DiningTable)
            .FirstOrDefaultAsync(x => x.Id == id
                && x.UserId == CurrentUserId
                && x.Status == "Confirmed"
                && x.DiningTableId != null);

        if (reservation == null)
        {
            TempData["Error"] = "The reservation must be confirmed before payment.";
            return RedirectToAction(nameof(AwaitingConfirmation), new { id });
        }

        if (await _db.Orders.AnyAsync(x => x.ReservationId == id && x.Status != "Cancelled"))
        {
            TempData["Error"] = "An order is already linked to this reservation.";
            return RedirectToAction(nameof(Index));
        }

        var cartLines = _cart.Get();
        if (cartLines.Count == 0)
        {
            TempData["Error"] = "Your cart expired. Add your meal again, then select this confirmed reservation at checkout.";
            return RedirectToAction("Index", "Menu");
        }

        var validation = await _cart.ValidateAsync();
        if (!validation.Success)
        {
            TempData["Error"] = validation.Message;
            return RedirectToAction("Index", "Cart");
        }

        var order = new Order
        {
            UserId = CurrentUserId.Value,
            Reservation = reservation,
            OrderType = "Dine-in",
            Notes = HttpContext.Session.GetString(CheckoutSession.DineInNotesKey),
            Status = "Pending",
            TotalAmount = cartLines.Sum(x => x.LineTotal),
            Items = cartLines.Select(x => new OrderItem
            {
                MenuItemId = x.MenuItemId,
                Quantity = x.Quantity,
                UnitPrice = x.Price,
                Sauce1Name = x.Sauce1,
                Sauce2Name = x.Sauce2
            }).ToList()
        };

        _db.Orders.Add(order);
        _db.Payments.Add(new Payment
        {
            UserId = CurrentUserId.Value,
            Order = order,
            Reservation = reservation,
            Amount = order.TotalAmount,
            Status = "Pending"
        });
        await _db.SaveChangesAsync();

        _cart.Clear();
        HttpContext.Session.Remove(CheckoutSession.DineInNotesKey);
        HttpContext.Session.Remove(CheckoutSession.CheckoutReservationIdKey);
        TempData["Success"] = $"{reservation.DiningTable!.TableNumber} is confirmed and linked to your order.";

        return RedirectToAction("Pay", "Payments", new { id = order.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!CurrentUserId.HasValue) return LoginRequired();

        var reservation = await _db.Reservations
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUserId);

        if (reservation != null && reservation.Status is "Pending" or "Accepted" or "Confirmed")
        {
            _automation.Release(reservation, "Cancelled");
            await _db.SaveChangesAsync();
            if (HttpContext.Session.GetInt32(CheckoutSession.CheckoutReservationIdKey) == reservation.Id)
                HttpContext.Session.Remove(CheckoutSession.CheckoutReservationIdKey);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadTableAvailabilityAsync(ReservationViewModel model, DateTime reservationTime)
    {
        var tables = await _db.DiningTables
            .Where(table => table.IsActive)
            .OrderBy(table => table.Capacity)
            .ThenBy(table => table.TableNumber)
            .ToListAsync();

        var start = reservationTime.AddHours(-2);
        var end = reservationTime.AddHours(2);
        var now = DateTime.Now;
        var reservedTableIds = await _db.Reservations
            .Where(reservation => reservation.DiningTableId != null
                && BlockingStatuses.Contains(reservation.Status)
                && (reservation.Status != "Pending"
                    || reservation.HoldExpiresAt.HasValue && reservation.HoldExpiresAt > now)
                && reservation.ReservationDate > start
                && reservation.ReservationDate < end)
            .Select(reservation => reservation.DiningTableId!.Value)
            .Distinct()
            .ToListAsync();

        model.Tables = tables.Select(table => new TableAvailabilityViewModel
        {
            Id = table.Id,
            TableNumber = table.TableNumber,
            Capacity = table.Capacity,
            State = table.Capacity < model.GuestCount
                ? "Too small"
                : reservedTableIds.Contains(table.Id) || IsOperationallyUnavailable(table, reservationTime) ? "Occupied" : "Available"
        }).ToList();
    }

    private Task<bool> HasConflictAsync(int tableId, DateTime reservationTime, int? exceptReservationId = null)
    {
        var start = reservationTime.AddHours(-2);
        var end = reservationTime.AddHours(2);

        var now = DateTime.Now;
        return _db.Reservations.AnyAsync(reservation =>
            reservation.Id != exceptReservationId
            && reservation.DiningTableId == tableId
            && BlockingStatuses.Contains(reservation.Status)
            && (reservation.Status != "Pending"
                || reservation.HoldExpiresAt.HasValue && reservation.HoldExpiresAt > now)
            && reservation.ReservationDate > start
            && reservation.ReservationDate < end);
    }

    private static bool IsOperationallyUnavailable(DiningTable table, DateTime reservationTime)
    {
        if (table.OperationalStatus == "Occupied" && table.OccupiedUntil.HasValue)
            return table.OccupiedUntil.Value.AddMinutes(30) > reservationTime;

        return table.OperationalStatus == "Cleaning" && reservationTime < DateTime.Now.AddMinutes(30);
    }
}
