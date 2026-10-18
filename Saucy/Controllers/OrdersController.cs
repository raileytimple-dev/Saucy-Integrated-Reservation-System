using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaucyMVC.Data;
using SaucyMVC.Models;

namespace SaucyMVC.Controllers;

public class OrdersController : BaseController
{
    private readonly SaucyDbContext _db;

    public OrdersController(SaucyDbContext db) => _db = db;

    public async Task<IActionResult> Track()
    {
        if (!CurrentUserId.HasValue)
        {
            ViewBag.IsGuestPreview = true;
            return View(null);
        }

        var order = await _db.Orders
            .Include(x => x.Reservation)
                .ThenInclude(reservation => reservation!.DiningTable)
            .Include(x => x.Items)
                .ThenInclude(item => item.MenuItem)
            .Include(x => x.StatusHistory)
            .AsSplitQuery()
            .Where(x => x.UserId == CurrentUserId && x.Status != "Completed" && x.Status != "Cancelled")
            .OrderByDescending(x => x.OrderedAt)
            .FirstOrDefaultAsync();

        return View(order);
    }

    public async Task<IActionResult> History()
    {
        if (!CurrentUserId.HasValue)
        {
            ViewBag.IsGuestPreview = true;
            return View(Enumerable.Empty<Order>());
        }

        var orders = await _db.Orders
            .Include(x => x.Reservation)
                .ThenInclude(reservation => reservation!.DiningTable)
            .Include(x => x.Items)
                .ThenInclude(item => item.MenuItem)
            .Include(x => x.StatusHistory)
            .AsSplitQuery()
            .Where(x => x.UserId == CurrentUserId)
            .OrderByDescending(x => x.OrderedAt)
            .ToListAsync();

        return View(orders);
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> LiveStatus(int? id)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();

        var query = _db.Orders
            .Include(order => order.StatusHistory)
            .Where(order => order.UserId == CurrentUserId.Value);

        Order? order;
        if (id.HasValue)
        {
            order = await query.FirstOrDefaultAsync(order => order.Id == id.Value);
        }
        else
        {
            order = await query
                .Where(order => order.Status != "Completed" && order.Status != "Cancelled")
                .OrderByDescending(order => order.OrderedAt)
                .FirstOrDefaultAsync();
        }

        if (order == null) return Json(new { found = false, serverTime = DateTime.Now });

        var history = order.StatusHistory
            .OrderByDescending(entry => entry.ChangedAt)
            .Select(entry => new
            {
                entry.Action,
                entry.FromStatus,
                entry.ToStatus,
                entry.ChangedAt,
                actor = entry.ChangedByRole == "Customer"
                    ? "You"
                    : $"Saucy {entry.ChangedByRole.ToLowerInvariant()}"
            })
            .ToList();

        return Json(new
        {
            found = true,
            order.Id,
            order.Status,
            order.CancellationRequestStatus,
            history,
            changedAt = history.FirstOrDefault()?.ChangedAt ?? order.OrderedAt,
            serverTime = DateTime.Now
        });
    }
}
