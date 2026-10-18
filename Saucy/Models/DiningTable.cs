using System.ComponentModel.DataAnnotations;

namespace SaucyMVC.Models;

public class DiningTable
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string TableNumber { get; set; } = "";

    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;

    [MaxLength(25)]
    public string OperationalStatus { get; set; } = "Available";

    public DateTime? OccupiedUntil { get; set; }
    public int? CurrentPartySize { get; set; }
}
