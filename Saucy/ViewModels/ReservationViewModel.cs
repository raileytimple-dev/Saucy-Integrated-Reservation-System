using System.ComponentModel.DataAnnotations;

namespace SaucyMVC.ViewModels;

public class ReservationViewModel
{
    [Required, DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today.AddDays(1);

    [Required, DataType(DataType.Time)]
    public TimeSpan Time { get; set; } = new(18, 0, 0);

    [Range(1, 30, ErrorMessage = "Party size must be between 1 and 30 people.")]
    public int GuestCount { get; set; } = 2;

    [Required(ErrorMessage = "Please choose an available table.")]
    public int? SelectedTableId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool ContinueToCheckout { get; set; }

    public IReadOnlyList<TableAvailabilityViewModel> Tables { get; set; } = [];
}

public class TableAvailabilityViewModel
{
    public int Id { get; init; }
    public string TableNumber { get; init; } = "";
    public int Capacity { get; init; }
    public string State { get; init; } = "Available";
    public bool IsAvailable => State == "Available";
}
