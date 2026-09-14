namespace CalorieTracker.Shared;

using System.ComponentModel.DataAnnotations;

public class FoodItemDto
{
    public string UserId { get; set; } = string.Empty;
    public string OriginalQuery { get; set; } = string.Empty;
    [Required(ErrorMessage = "Food name cannot be empty.")]
    [MaxLength(100, ErrorMessage = "Food name can be a maximum of 100 characters.")]
    public string FoodName { get; set; } = string.Empty;
    public string Portion { get; set; } = string.Empty;
    [Range(0, 10000, ErrorMessage = "Calories must be between 0 and 10000.")]
    public double Calories { get; set; }
    public double ProteinGrams { get; set; }
    public double CarbsGrams { get; set; }
    public double FatGrams { get; set; } 
    public MealType MealType { get; set; }
    public bool IsFound { get; set; }
    public int Id { get; set; } 
    public DateTime ConsumedDate { get; set; } = DateTime.Today;
}
