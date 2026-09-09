using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Net.Http.Json;
using System.Net.Http.Headers;
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
    
    // MAUI projesini fiziksel cihazda çalıştırırken localhost yerine bilgisayarının yerel IP'sini yazmalısın.
    // Şimdilik 10.0.2.2 kullanıyorum (Android Emülatörü için localhost karşılığı).
    // Fiziksel cihaz için "192.168.1.X" gibi bir adres yazmalısın.
	private readonly string _baseUrl = "http://localhost:5119";
	private DateTime _currentViewDate = DateTime.Today;
	private bool _isGlowBreathing = false;
	private Color _currentGlowColor = Colors.Transparent;
	private int _dateChangeClickCount = 0;

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
    }
    
    private async Task<string> GetOrCreateTokenAsync()
    {
        var existingToken = await SecureStorage.Default.GetAsync("auth_token");
        
        if (!string.IsNullOrEmpty(existingToken))
        {
            return existingToken; 
        }

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/token", null);
        response.EnsureSuccessStatusCode();

        var authResult = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        
        if(authResult != null && !string.IsNullOrEmpty(authResult.Token))
        {
             await SecureStorage.Default.SetAsync("auth_token", authResult.Token);
             return authResult.Token;
        }
        
        return string.Empty;
    }
    
    private async Task EnsureAuthorizedClient()
    {
        var token = await GetOrCreateTokenAsync();
        if(!string.IsNullOrEmpty(token))
        {
             _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
	private async void OnSortClicked(object? sender, EventArgs e)
	{
		if (FoodItems.Count == 0) return;

		string action = await DisplayActionSheetAsync("Sıralama Ölçütü", "İptal", null, "Öğüne Göre", "Kaloriye Göre (En Yüksek)", "Proteine Göre (En Yüksek)", "Karbonhidrata Göre (En Yüksek)", "Yağa Göre (En Yüksek)", "İsme Göre (A-Z)");

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

    private async void OnAddFoodClicked(object? sender, EventArgs e)
	{
		string userInput = FoodInput.Text;
		if (string.IsNullOrWhiteSpace(userInput)) return;

		try
		{
            await EnsureAuthorizedClient();

			string dateQuery = _currentViewDate.ToString("yyyy-MM-dd");
			string url = $"{_baseUrl}/api/analyze-food?date={dateQuery}";

			var response = await _httpClient.PostAsJsonAsync(url, userInput);

			if (response.IsSuccessStatusCode)
			{
				var returnedFoods = await response.Content.ReadFromJsonAsync<List<FoodItemDto>>();
				if (returnedFoods != null)
				{
					foreach (var food in returnedFoods) FoodItems.Add(food);
					RecalculateTotals();
					FoodInput.Text = string.Empty;
				}
			}
			else
			{
				string errorDetail = await response.Content.ReadAsStringAsync();
				await DisplayAlertAsync("API Hatası", $"Kodu: {response.StatusCode}\nDetay: {errorDetail}", "Tamam");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Bağlantı Hatası", ex.Message, "Tamam");
		}
	}

	private void RecalculateTotals()
	{
		TotalCalories = FoodItems.Sum(f => f.Calories);
		TotalProtein = FoodItems.Sum(f => f.ProteinGrams);
		TotalCarbs = FoodItems.Sum(f => f.CarbsGrams);
		TotalFat = FoodItems.Sum(f => f.FatGrams);

		UpdateDynamicEffects(TotalCalories);
	}

	private async void OnFoodItemSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is not FoodItemDto selectedFood)
			return;

		string action = await DisplayActionSheetAsync($"{selectedFood.FoodName} İşlemleri", "İptal", "Sil", "Gramajı Ayarla", "Öğünü Değiştir");

		if (action == "Sil")
		{
			FoodItems.Remove(selectedFood);
			RecalculateTotals();
			
            await EnsureAuthorizedClient();
			await _httpClient.DeleteAsync($"{_baseUrl}/api/delete-food/{selectedFood.Id}");
		}
		else if (action == "Gramajı Ayarla")
		{
			var match = System.Text.RegularExpressions.Regex.Match(selectedFood.Portion, @"[0-9]+([.,][0-9]+)?");
			double currentAmount = 1; 
			string unit = selectedFood.Portion;

			if (match.Success)
			{
				double.TryParse(match.Value.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out currentAmount);
				unit = selectedFood.Portion.Replace(match.Value, "").Trim();
			}

			string result = await DisplayPromptAsync("Gramaj Ayarla", $"Mevcut: {selectedFood.Portion}\nYeni miktarı girin ({unit}):", initialValue: currentAmount.ToString());
			
			if (!string.IsNullOrWhiteSpace(result) && double.TryParse(result.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double newAmount) && newAmount > 0)
			{
				double multiplier = newAmount / currentAmount;

				var updatedFood = new FoodItemDto
				{
					Id = selectedFood.Id,
					ConsumedDate = selectedFood.ConsumedDate,
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

                await EnsureAuthorizedClient();
				await _httpClient.PutAsJsonAsync($"{_baseUrl}/api/update-food/{updatedFood.Id}", updatedFood);
			}
		}
		else if (action == "Öğünü Değiştir")
		{
			string mealAction = await DisplayActionSheetAsync("Öğün Seç", "İptal", null, "Sabah", "Ogle", "Aksam", "AraOgun", "BilinmeyenOgun");
			
			if (mealAction != "İptal" && !string.IsNullOrEmpty(mealAction))
			{
				var updatedFood = new FoodItemDto
				{
					Id = selectedFood.Id,
					ConsumedDate = selectedFood.ConsumedDate,
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

                await EnsureAuthorizedClient();
				await _httpClient.PutAsJsonAsync($"{_baseUrl}/api/update-food/{updatedFood.Id}", updatedFood);
			}
		}

		if (sender is CollectionView collectionView)
		{
			collectionView.SelectedItem = null;
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadFoodsForDate(_currentViewDate);
	}

	private async Task LoadFoodsForDate(DateTime targetDate)
	{
		UpdateDateUI();
		FoodItems.Clear();

		try
		{
            await EnsureAuthorizedClient();

			string dateString = targetDate.ToString("yyyy-MM-dd");
			var savedFoods = await _httpClient.GetFromJsonAsync<List<FoodItemDto>>($"{_baseUrl}/api/get-foods/{dateString}");

			if (savedFoods != null)
			{
				foreach (var food in savedFoods) FoodItems.Add(food);
				RecalculateTotals();
			}
		}
		catch {}
	}

	private void UpdateDateUI()
	{
		bool canGoBack = _currentViewDate > DateTime.Today.AddDays(-7);
		PrevDayButton.IsEnabled = canGoBack;
		PrevDayButton.TextColor = canGoBack ? Color.FromArgb("#B900FF") : Color.FromArgb("#444444");
		
		bool canGoForward = _currentViewDate < DateTime.Today;
		NextDayButton.IsEnabled = canGoForward;
		NextDayButton.TextColor = canGoForward ? Color.FromArgb("#B900FF") : Color.FromArgb("#444444");

		if (_currentViewDate == DateTime.Today)
			DateLabel.Text = "BUGÜN";
		else if (_currentViewDate == DateTime.Today.AddDays(-1))
			DateLabel.Text = "DÜN";
		else
			DateLabel.Text = _currentViewDate.ToString("dd MMMM dddd").ToUpper();
	}

	private async void OnPrevDayClicked(object? sender, EventArgs e)
	{
		_currentViewDate = _currentViewDate.AddDays(-1);
		
		var currentClick = ++_dateChangeClickCount;

		await Task.Delay(300);

		if (currentClick == _dateChangeClickCount)
		{
			await LoadFoodsForDate(_currentViewDate);
		}
	}

	private async void OnNextDayClicked(object? sender, EventArgs e)
	{
		_currentViewDate = _currentViewDate.AddDays(1);
		
		var currentClick = ++_dateChangeClickCount;

		await Task.Delay(300);

		if (currentClick == _dateChangeClickCount)
		{
			await LoadFoodsForDate(_currentViewDate);
		}
	}
	private void UpdateDynamicEffects(double totalCalories)
	{
		Color targetColor = GetTargetGlowColor(totalCalories);

		if (totalCalories >= 500 && GlowEffect.Opacity == 0)
		{
			GlowEffect.FadeToAsync(0.8, 1000, Easing.CubicOut);
		}
		else if (totalCalories < 500)
		{
			GlowEffect.FadeToAsync(0, 1000, Easing.CubicOut);
			_isGlowBreathing = false;
		}

		if (totalCalories >= 500)
		{
			AnimateColorTransition(targetColor);
		}

		if (totalCalories >= 500 && !_isGlowBreathing)
		{
			_isGlowBreathing = true;
			StartBreathingAnimation();
		}
	}

	private Color GetTargetGlowColor(double calories)
	{
		if (calories < 500) return Colors.Transparent;
		
		if (calories >= 500 && calories <= 1000) return Color.FromArgb("#00FF00"); 

		if (calories > 1000 && calories <= 2000)
			return LerpColor(Color.FromArgb("#00FF00"), Color.FromArgb("#FFFF00"), (calories - 1000) / 1000.0);

		if (calories > 2000 && calories <= 3000)
			return LerpColor(Color.FromArgb("#FFFF00"), Color.FromArgb("#FF0000"), (calories - 2000) / 1000.0);

		return Color.FromArgb("#FF0000"); 
	}

	private Color LerpColor(Color start, Color end, double fraction)
	{
		var r = start.Red + (end.Red - start.Red) * fraction;
		var g = start.Green + (end.Green - start.Green) * fraction;
		var b = start.Blue + (end.Blue - start.Blue) * fraction;
		return Color.FromRgba(r, g, b, 1.0);
	}

	private void AnimateColorTransition(Color targetColor)
	{
		if (_currentGlowColor == targetColor) return;

		var startColor = _currentGlowColor;
		this.AbortAnimation("GlowColorAnim"); 

		var animation = new Animation(v =>
		{
			var r = startColor.Red + (targetColor.Red - startColor.Red) * v;
			var g = startColor.Green + (targetColor.Green - startColor.Green) * v;
			var b = startColor.Blue + (targetColor.Blue - startColor.Blue) * v;
			
			_currentGlowColor = Color.FromRgba(r, g, b, 1.0);
			
			GlowCenter.Color = _currentGlowColor.WithAlpha(0.25f); 
			GlowMid.Color = _currentGlowColor.WithAlpha(0.08f);
		}, 0, 1);

		animation.Commit(this, "GlowColorAnim", 16, 800, Easing.CubicOut);
	}

	private async void StartBreathingAnimation()
	{
		while (_isGlowBreathing)
		{
			await Task.WhenAll(
				GlowEffect.ScaleToAsync(1.05, 1800, Easing.SinInOut),
				GlowEffect.FadeToAsync(0.9, 1800, Easing.SinInOut)
			);

			if (!_isGlowBreathing) break;

			await Task.WhenAll(
				GlowEffect.ScaleToAsync(0.95, 1800, Easing.SinInOut),
				GlowEffect.FadeToAsync(0.6, 1800, Easing.SinInOut)
			);
		}
	}
}