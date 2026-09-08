namespace CalorieTracker.Shared;

using System.ComponentModel.DataAnnotations;

public class FoodItemDto
{
    public string OriginalQuery { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yiyecek ismi boş bırakılamaz.")]
    [MaxLength(100, ErrorMessage = "Yiyecek ismi maksimum 100 karakter olabilir.")]
    public string FoodName { get; set; } = string.Empty;
    public string Portion { get; set; } = string.Empty; 
    
    [Range(0, 10000, ErrorMessage = "Kalori 0 ile 10000 arasında olmalıdır.")]
    public double Calories { get; set; }
    public double ProteinGrams { get; set; }
    public double CarbsGrams { get; set; }
    public double FatGrams { get; set; } 
    public MealType MealType { get; set; }
    public bool IsFound { get; set; }
    public string DisplayMealType 
    { 
        get 
        {
            return MealType switch
            {
                MealType.Sabah => "Sabah",
                MealType.Ogle => "Öğle",
                MealType.Aksam => "Akşam",
                MealType.AraOgun => "Ara\nÖğün",
                MealType.BilinmeyenOgun => "Bilinmeyen\nÖğün",
                _ => MealType.ToString()
            };
        } 
    }
    public int Id { get; set; } 
    public DateTime ConsumedDate { get; set; } = DateTime.Today;
}
