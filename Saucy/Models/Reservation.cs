using System.ComponentModel.DataAnnotations;
namespace SaucyMVC.Models;
public class Reservation {
 public int Id{get;set;} public int UserId{get;set;} public User? User{get;set;}
 public int? DiningTableId{get;set;} public DiningTable? DiningTable{get;set;}
 public DateTime ReservationDate{get;set;} [Range(1,30)] public int GuestCount{get;set;}
 [MaxLength(25)] public string Status{get;set;}="Pending"; [MaxLength(500)] public string? Notes{get;set;}
 [MaxLength(400)] public string? PaymentProofPath{get;set;} public DateTime CreatedAt{get;set;}=DateTime.Now;
 public DateTime? HoldExpiresAt{get;set;}
 public DateTime? ConfirmedAt{get;set;}
 public DateTime? CheckedInAt{get;set;}
 public DateTime? ReleasedAt{get;set;}
}
