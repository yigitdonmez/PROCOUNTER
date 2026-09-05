using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Net.Http.Json;
using CalorieTracker.Shared;

namespace CalorieTracker.Mobile;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
    public ObservableCollection<FoodItemDto> FoodItems { get; set; } = new();

    private double _totalCalories;
    public double TotalCalories { get => _totalCalories; set { _totalCalories = value; OnPropertyChanged(); } }

    private double _totalProtein;
    public double TotalProtein { get => _totalProtein; set { _totalProtein = value; OnPropertyChanged(); } }

    private double _totalCarbs;
    public double TotalCarbs { get => _totalCarbs; set { _totalCarbs = value; OnPropertyChanged(); } }

    private double _totalFat;
    public double TotalFat { get => _totalFat; set { _totalFat = value; OnPropertyChanged(); } }

    private readonly HttpClient _httpClient = new();

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

	private async void OnSortClicked(object sender, EventArgs e)
	{
		if (FoodItems.Count == 0) return;

		string action = await DisplayActionSheet("Sıralama Ölçütü", "İptal", null, "Öğüne Göre", "Kaloriye Göre (En Yüksek)", "Proteine Göre (En Yüksek)", "Karbonhidrata Göre (En Yüksek)", "Yağa Göre (En Yüksek)", "İsme Göre (A-Z)");

		if (action == "İptal" || string.IsNullOrEmpty(action)) return;

		List<FoodItemDto> sortedList = new();

		switch (action)
		{
			case "Öğüne Göre":
				sortedList = FoodItems.OrderBy(f => f.MealType).ToList(); 
				break;
			case "Kaloriye Göre (En Yüksek)":
				sortedList = FoodItems.OrderByDescending(f => f.Calories).ToList();
				break;
			case "Proteine Göre (En Yüksek)":
				sortedList = FoodItems.OrderByDescending(f => f.ProteinGrams).ToList();
				break;
			case "Karbonhidrata Göre (En Yüksek)":
				sortedList = FoodItems.OrderByDescending(f => f.CarbsGrams).ToList();
				break;
			case "Yağa Göre (En Yüksek)":
				sortedList = FoodItems.OrderByDescending(f => f.FatGrams).ToList();
				break;
			case "İsme Göre (A-Z)":
				sortedList = FoodItems.OrderBy(f => f.FoodName).ToList();
				break;
		}

		FoodItems.Clear();
		foreach (var item in sortedList)
		{
			FoodItems.Add(item);
		}
	}

    private async void OnAddFoodClicked(object sender, EventArgs e)
	{
		string userInput = FoodInput.Text;
		if (string.IsNullOrWhiteSpace(userInput)) return;

		FoodInput.IsEnabled = false;

		try
		{
			var response = await _httpClient.PostAsJsonAsync("http://localhost:5119/api/analyze-food", userInput);
			
			if (response.IsSuccessStatusCode)
			{
				var returnedFoods = await response.Content.ReadFromJsonAsync<List<FoodItemDto>>();
				
				if (returnedFoods != null)
				{
					foreach (var food in returnedFoods)
					{
						FoodItems.Add(food);

						TotalCalories += food.Calories;
						TotalProtein += food.ProteinGrams;
						TotalCarbs += food.CarbsGrams;
						TotalFat += food.FatGrams;
					}
				}
				FoodInput.Text = string.Empty;
			}
			else
			{
				string errorContent = await response.Content.ReadAsStringAsync();
				string displayMessage = "API'den başarısız yanıt döndü.";

				if (errorContent.Contains("sınırına ulaşıldı") || (int)response.StatusCode == 429)
				{
					displayMessage = "Gemini API sınırına (Rate Limit) ulaşıldı. Lütfen 1-2 dakika bekleyip tekrar deneyin.";
				}
				else
				{
					displayMessage = $"Sunucu Hatası: {response.StatusCode}";
				}

				await DisplayAlert("Hata", displayMessage, "Tamam");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Bağlantı Hatası", "API'ye ulaşılamadı. Sunucunun çalıştığından emin olun.\nDetay: " + ex.Message, "Tamam");
		}
		finally
		{
			FoodInput.IsEnabled = true;
		}
	}

	private void RecalculateTotals()
	{
		TotalCalories = FoodItems.Sum(f => f.Calories);
		TotalProtein = FoodItems.Sum(f => f.ProteinGrams);
		TotalCarbs = FoodItems.Sum(f => f.CarbsGrams);
		TotalFat = FoodItems.Sum(f => f.FatGrams);
	}

	private async void OnFoodItemSelected(object sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is not FoodItemDto selectedFood)
			return;

		string action = await DisplayActionSheet($"{selectedFood.FoodName} İşlemleri", "İptal", "Sil", "Gramajı Ayarla", "Öğünü Değiştir");

		if (action == "Sil")
		{
			FoodItems.Remove(selectedFood);
			RecalculateTotals();
		}
		else if (action == "Gramajı Ayarla")
		{
			var match = Regex.Match(selectedFood.Portion, @"[0-9]+([.,][0-9]+)?");
			double currentAmount = 1;
			string unit = selectedFood.Portion;

			if (match.Success)
			{
				double.TryParse(match.Value.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out currentAmount);
				unit = selectedFood.Portion.Replace(match.Value, "").Trim();
			}

			string result = await DisplayPromptAsync("Gramaj Ayarla", 
				$"Mevcut: {selectedFood.Portion}\nYeni miktarı girin ({unit}):", 
				initialValue: currentAmount.ToString());
			
			if (!string.IsNullOrWhiteSpace(result) && double.TryParse(result.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double newAmount) && newAmount > 0)
			{
				double multiplier = newAmount / currentAmount;

				var updatedFood = new FoodItemDto
				{
					OriginalQuery = selectedFood.OriginalQuery,
					FoodName = selectedFood.FoodName,
					IsFound = selectedFood.IsFound,
					MealType = selectedFood.MealType,
					Portion = $"{newAmount} {unit}".Trim(),
					Calories = selectedFood.Calories * multiplier,
					ProteinGrams = selectedFood.ProteinGrams * multiplier,
					CarbsGrams = selectedFood.CarbsGrams * multiplier,
					FatGrams = selectedFood.FatGrams * multiplier
				};

				int index = FoodItems.IndexOf(selectedFood);
				FoodItems[index] = updatedFood;

				RecalculateTotals();
			}
		}
		else if (action == "Öğünü Değiştir")
		{
			string mealAction = await DisplayActionSheet("Öğün Seç", "İptal", null, "Sabah", "Ogle", "Aksam", "AraOgun", "BilinmeyenOgun");
			
			if (mealAction != "İptal" && !string.IsNullOrEmpty(mealAction))
			{
				var updatedFood = new FoodItemDto
				{
					OriginalQuery = selectedFood.OriginalQuery,
					FoodName = selectedFood.FoodName,
					IsFound = selectedFood.IsFound,
					Portion = selectedFood.Portion,
					Calories = selectedFood.Calories,
					ProteinGrams = selectedFood.ProteinGrams,
					CarbsGrams = selectedFood.CarbsGrams,
					FatGrams = selectedFood.FatGrams,
					MealType = Enum.Parse<MealType>(mealAction)
				};

				int index = FoodItems.IndexOf(selectedFood);
				FoodItems[index] = updatedFood;
			}
		}

		((CollectionView)sender).SelectedItem = null;
	}
}