using System.ComponentModel.DataAnnotations;

namespace SaucyMVC.Models;

public class SauceOption
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = "";

    [Required, MaxLength(400)]
    public string ImagePath { get; set; } = "";

    public bool IsAvailable { get; set; } = true;
    public int DisplayOrder { get; set; }
}
