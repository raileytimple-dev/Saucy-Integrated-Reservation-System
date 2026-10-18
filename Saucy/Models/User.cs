using System.ComponentModel.DataAnnotations;
namespace SaucyMVC.Models;
public class User {
 public int Id { get; set; }
 [Required,MaxLength(120)] public string FullName { get; set; }="";
 [Required,EmailAddress,MaxLength(160)] public string Email { get; set; }="";
 [MaxLength(30)] public string PhoneNumber { get; set; }="";
 [Required] public string PasswordHash { get; set; }="";
 [Required,MaxLength(20)] public string Role { get; set; }="Customer";
 public bool IsActive { get; set; }=true;
 public DateTime CreatedAt { get; set; }=DateTime.Now;
 public DateTime? LastLoginAt { get; set; }
}
