using System.Reflection;
using System.Resources;

namespace CalorieTracker.Mobile.Resources.Strings;

public static class AppResources
{
    private static readonly ResourceManager ResourceManager = 
        new ResourceManager("CalorieTracker.Mobile.Resources.Strings.AppResources", typeof(AppResources).Assembly);

    private static string GetString(string name) => ResourceManager.GetString(name) ?? name;

    public static string Today => GetString("Today");
    public static string Yesterday => GetString("Yesterday");
    public static string DailyTotal => GetString("DailyTotal");
    public static string Kcal => GetString("Kcal");
    public static string Protein => GetString("Protein");
    public static string Carbs => GetString("Carbs");
    public static string Fat => GetString("Fat");
    public static string FoodInputPlaceholder => GetString("FoodInputPlaceholder");
    public static string Add => GetString("Add");
    public static string Consumed => GetString("Consumed");
    public static string Sort => GetString("Sort");
    public static string SortCriteria => GetString("SortCriteria");
    public static string Cancel => GetString("Cancel");
    public static string ByMeal => GetString("ByMeal");
    public static string ByCalorieDesc => GetString("ByCalorieDesc");
    public static string ByProteinDesc => GetString("ByProteinDesc");
    public static string ByCarbsDesc => GetString("ByCarbsDesc");
    public static string ByFatDesc => GetString("ByFatDesc");
    public static string ByName => GetString("ByName");
    public static string ApiError => GetString("ApiError");
    public static string Code => GetString("Code");
    public static string Detail => GetString("Detail");
    public static string Ok => GetString("Ok");
    public static string ConnectionError => GetString("ConnectionError");
    public static string ActionsFor => GetString("ActionsFor");
    public static string Delete => GetString("Delete");
    public static string AdjustWeight => GetString("AdjustWeight");
    public static string ChangeMeal => GetString("ChangeMeal");
    public static string Current => GetString("Current");
    public static string EnterNewAmount => GetString("EnterNewAmount");
    public static string SelectMeal => GetString("SelectMeal");
    public static string Morning => GetString("Morning");
    public static string Noon => GetString("Noon");
    public static string Evening => GetString("Evening");
    public static string Snack => GetString("Snack");
    public static string UnknownMeal => GetString("UnknownMeal");
    public static string ServerUnreachable => GetString("ServerUnreachable");
    public static string DataError => GetString("DataError");
    public static string RecordsLoadError => GetString("RecordsLoadError");
}