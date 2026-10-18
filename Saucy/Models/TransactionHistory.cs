namespace SaucyMVC.Models;
public class TransactionHistory { public int Id{get;set;} public int UserId{get;set;} public User? User{get;set;} public int? OrderId{get;set;} public decimal TotalAmount{get;set;} public string OrderItems{get;set;}=""; public string PaymentMethod{get;set;}=""; public DateTime TransactionDate{get;set;}=DateTime.Now; }
