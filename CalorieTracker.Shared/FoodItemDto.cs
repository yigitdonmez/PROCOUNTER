namespace CalorieTracker.Shared;

public class FoodItemDto
{
    public string OriginalQuery { get; set; } = string.Empty;
    public string FoodName { get; set; } = string.Empty;
    public string Portion { get; set; } = string.Empty; 
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
}