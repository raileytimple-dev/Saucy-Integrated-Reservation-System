using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SaucyMVC.Data;
using SaucyMVC.Models;

namespace SaucyMVC.Services;

public sealed record ReservationAutomationResult(int ExpiredHolds, int ReleasedNoShows, int TablesMovedToCleaning)
{
    public int TotalChanges => ExpiredHolds + ReleasedNoShows + TablesMovedToCleaning;
}

public sealed class ReservationAutomationService
{
    private readonly SaucyDbContext _db;
    private readonly OrderWorkflowService _orderWorkflow;
    private readonly ReservationAutomationOptions _options;

    public ReservationAutomationService(
        SaucyDbContext db,
        OrderWorkflowService orderWorkflow,
        IOptions<ReservationAutomationOptions> options)
    {
        _db = db;
        _orderWorkflow = orderWorkflow;
        _options = options.Value;
    }

    public int PendingHoldMinutes => Math.Clamp(_options.PendingHoldMinutes, 5, 120);
    public int NoShowGraceMinutes => Math.Clamp(_options.NoShowGraceMinutes, 5, 120);
    public int WalkInTurnoverBufferMinutes => Math.Clamp(_options.WalkInTurnoverBufferMinutes, 0, 120);
    public int DefaultDiningMinutes => Math.Clamp(_options.DefaultDiningMinutes, 30, 240);

    public DateTime BeginHold(Reservation reservation, DateTime? now = null)
    {
        var expiresAt = (now ?? DateTime.Now).AddMinutes(PendingHoldMinutes);
        reservation.Status = "Pending";
        reservation.HoldExpiresAt = expiresAt;
        reservation.ConfirmedAt = null;
        reservation.CheckedInAt = null;
        reservation.ReleasedAt = null;
        return expiresAt;
    }

    public bool IsLiveHold(Reservation reservation, DateTime? now = null) =>
        reservation.Status == "Pending"
        && reservation.HoldExpiresAt.HasValue
        && reservation.HoldExpiresAt.Value > (now ?? DateTime.Now);

    public bool Confirm(Reservation reservation, DateTime? now = null)
    {
        var changedAt = now ?? DateTime.Now;
        if (reservation.Status != "Pending" || !IsLiveHold(reservation, changedAt)) return false;

        reservation.Status = "Confirmed";
        reservation.ConfirmedAt = changedAt;
        reservation.HoldExpiresAt = null;
        reservation.ReleasedAt = null;
        return true;
    }

    public void Seat(Reservation reservation, DateTime? now = null)
    {
        var checkedInAt = now ?? DateTime.Now;
        reservation.Status = "Seated";
        reservation.CheckedInAt = checkedInAt;
        reservation.ReleasedAt = null;
    }

    public void Release(Reservation reservation, string status, DateTime? now = null)
    {
        reservation.Status = status;
        reservation.HoldExpiresAt = null;
        reservation.ReleasedAt = now ?? DateTime.Now;
    }

    public async Task<ReservationAutomationResult> ProcessAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var expiredHolds = await _db.Reservations
            .Where(reservation => reservation.Status == "Pending"
                && reservation.HoldExpiresAt.HasValue
                && reservation.HoldExpiresAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var reservation in expiredHolds)
            Release(reservation, "Expired", now);

        var noShowCutoff = now.AddMinutes(-NoShowGraceMinutes);
        var noShows = await _db.Reservations
            .Where(reservation => reservation.Status == "Confirmed"
                && reservation.ReservationDate <= noShowCutoff)
            .ToListAsync(cancellationToken);

        foreach (var reservation in noShows)
            Release(reservation, "No-show", now);

        if (noShows.Count > 0)
        {
            var releasedReservationIds = noShows.Select(reservation => reservation.Id).ToList();
            var activeOrders = await _db.Orders
                .Where(order => order.ReservationId.HasValue
                    && releasedReservationIds.Contains(order.ReservationId.Value)
                    && order.Status != "Completed"
                    && order.Status != "Cancelled")
                .ToListAsync(cancellationToken);

            foreach (var order in activeOrders)
                _orderWorkflow.RecordEvent(order, "Reserved table auto-released", null, "System", "System",
                    $"The customer did not check in within the {NoShowGraceMinutes}-minute grace period. The food order remains active for staff review.");
        }

        var expiredTables = await _db.DiningTables
            .Where(table => table.OperationalStatus == "Occupied"
                && table.OccupiedUntil.HasValue
                && table.OccupiedUntil <= now)
            .ToListAsync(cancellationToken);

        foreach (var table in expiredTables)
        {
            table.OperationalStatus = "Cleaning";
            table.OccupiedUntil = null;
            table.CurrentPartySize = null;
        }

        if (expiredHolds.Count > 0 || noShows.Count > 0 || expiredTables.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return new ReservationAutomationResult(expiredHolds.Count, noShows.Count, expiredTables.Count);
    }
}
