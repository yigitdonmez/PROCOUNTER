using System.Globalization;
using CalorieTracker.Shared;
using CalorieTracker.Mobile.Resources.Strings;
using Microsoft.Maui.Controls;

namespace CalorieTracker.Mobile.Converters;

public class MealTypeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;

        try
        {
            int mealInt = (int)value;
            
            return mealInt switch
            {
                (int)MealType.Sabah => AppResources.Morning,
                (int)MealType.Ogle => AppResources.Noon,
                (int)MealType.Aksam => AppResources.Evening,
                (int)MealType.AraOgun => AppResources.Snack,
                _ => AppResources.UnknownMeal
            };
        }
        catch
        {
            return value.ToString() ?? "?";
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value ?? string.Empty; 
    }
}