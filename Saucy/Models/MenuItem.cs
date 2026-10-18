using System.ComponentModel.DataAnnotations;
namespace SaucyMVC.Models;
public class MenuItem { public int Id{get;set;} [Required,MaxLength(120)] public string Name{get;set;}=""; [MaxLength(60)] public string Category{get;set;}="Famous Offerings"; [MaxLength(500)] public string Description{get;set;}=""; public decimal Price{get;set;} public bool IsAvailable{get;set;}=true; [MaxLength(400)] public string? ImagePath{get;set;} }
