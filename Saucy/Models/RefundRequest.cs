namespace SaucyMVC.Models;
public class RefundRequest { public int Id{get;set;} public int UserId{get;set;} public User? User{get;set;} public int OrderId{get;set;} public Order? Order{get;set;} public decimal Amount{get;set;} public string Description{get;set;}=""; public string Status{get;set;}="Pending"; public DateTime RequestedAt{get;set;}=DateTime.Now; }
