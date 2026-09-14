using System.Globalization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using CalorieTracker.Shared;
using CalorieTracker.Mobile.Resources.Strings; // Dil dosyası entegre edildi

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
    private readonly HttpClient _httpClient;
	private readonly IHttpClientFactory _httpClientFactory;
	private DateTime _currentViewDate = DateTime.Today;
	private bool _isGlowBreathing = false;
	private Color _currentGlowColor = Colors.Transparent;
	private int _dateChangeClickCount = 0;

    public MainPage(IHttpClientFactory httpClientFactory)
    {
		_httpClientFactory = httpClientFactory;

        string savedLanguage = Preferences.Default.Get("AppLanguage", "en");
        var culture = new CultureInfo(savedLanguage);
        
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        InitializeComponent();
        BindingContext = this;
		_httpClient = httpClientFactory.CreateClient("CalorieApi");

		MainDatePicker.MaximumDate = DateTime.Today;
		MainDatePicker.MinimumDate = DateTime.Today.AddYears(-1);
    }
    
    private async Task<string> GetOrCreateTokenAsync()
    {
        var existingToken = await SecureStorage.Default.GetAsync("auth_token");
        if (!string.IsNullOrEmpty(existingToken)) return existingToken; 

        var response = await _httpClient.PostAsync($"/api/token", null);
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

		string action = await DisplayActionSheetAsync(AppResources.SortCriteria, AppResources.Cancel, null, 
            AppResources.ByMeal, AppResources.ByCalorieDesc, AppResources.ByProteinDesc, AppResources.ByCarbsDesc, AppResources.ByFatDesc, AppResources.ByName);

		if (action == AppResources.Cancel || string.IsNullOrEmpty(action)) return;

		List<FoodItemDto> sortedList = new();

        if (action == AppResources.ByMeal) sortedList = FoodItems.OrderBy(f => f.MealType).ToList();
        else if (action == AppResources.ByCalorieDesc) sortedList = FoodItems.OrderByDescending(f => f.Calories).ToList();
        else if (action == AppResources.ByProteinDesc) sortedList = FoodItems.OrderByDescending(f => f.ProteinGrams).ToList();
        else if (action == AppResources.ByCarbsDesc) sortedList = FoodItems.OrderByDescending(f => f.CarbsGrams).ToList();
        else if (action == AppResources.ByFatDesc) sortedList = FoodItems.OrderByDescending(f => f.FatGrams).ToList();
        else if (action == AppResources.ByName) sortedList = FoodItems.OrderBy(f => f.FoodName).ToList();

		FoodItems.Clear();
		foreach (var item in sortedList) FoodItems.Add(item);
	}

    private async void OnAddFoodClicked(object? sender, EventArgs e)
	{
		string userInput = FoodInput.Text;
		if (string.IsNullOrWhiteSpace(userInput)) return;

		try
		{
            await EnsureAuthorizedClient();
			string dateQuery = _currentViewDate.ToString("yyyy-MM-dd");
			var response = await _httpClient.PostAsJsonAsync($"/api/analyze-food?date={dateQuery}", userInput);

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
				await DisplayAlertAsync(AppResources.ApiError, $"{AppResources.Code}: {response.StatusCode}\n{AppResources.Detail}: {errorDetail}", AppResources.Ok);
			}
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync(AppResources.ConnectionError, ex.Message, AppResources.Ok);
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
		if (e.CurrentSelection.FirstOrDefault() is not FoodItemDto selectedFood) return;

		string action = await DisplayActionSheetAsync(string.Format(AppResources.ActionsFor, selectedFood.FoodName), AppResources.Cancel, AppResources.Delete, AppResources.AdjustWeight, AppResources.ChangeMeal);

		if (action == AppResources.Delete)
		{
			FoodItems.Remove(selectedFood);
			RecalculateTotals();
            await EnsureAuthorizedClient();
			await _httpClient.DeleteAsync($"/api/delete-food/{selectedFood.Id}");
		}
		else if (action == AppResources.AdjustWeight)
		{
			var match = System.Text.RegularExpressions.Regex.Match(selectedFood.Portion, @"[0-9]+([.,][0-9]+)?");
			double currentAmount = 1; 
			string unit = selectedFood.Portion;

			if (match.Success)
			{
				double.TryParse(match.Value.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out currentAmount);
				unit = selectedFood.Portion.Replace(match.Value, "").Trim();
			}

			string result = await DisplayPromptAsync(AppResources.AdjustWeight, $"{AppResources.Current}: {selectedFood.Portion}\n{AppResources.EnterNewAmount} ({unit}):", initialValue: currentAmount.ToString());
			
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
				await _httpClient.PutAsJsonAsync($"/api/update-food/{updatedFood.Id}", updatedFood);
			}
		}
		else if (action == AppResources.ChangeMeal)
		{
			string mealAction = await DisplayActionSheetAsync(AppResources.SelectMeal, AppResources.Cancel, null, 
                AppResources.Morning, AppResources.Noon, AppResources.Evening, AppResources.Snack, AppResources.UnknownMeal);
			
			if (mealAction != AppResources.Cancel && !string.IsNullOrEmpty(mealAction))
			{
                MealType parsedType = MealType.BilinmeyenOgun;
                if (mealAction == AppResources.Morning) parsedType = MealType.Sabah;
                else if (mealAction == AppResources.Noon) parsedType = MealType.Ogle;
                else if (mealAction == AppResources.Evening) parsedType = MealType.Aksam;
                else if (mealAction == AppResources.Snack) parsedType = MealType.AraOgun;

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
					MealType = parsedType
				};

				int index = FoodItems.IndexOf(selectedFood);
				FoodItems[index] = updatedFood;
                await EnsureAuthorizedClient();
				await _httpClient.PutAsJsonAsync($"/api/update-food/{updatedFood.Id}", updatedFood);
			}
		}

		if (sender is CollectionView collectionView) collectionView.SelectedItem = null;
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
			var savedFoods = await _httpClient.GetFromJsonAsync<List<FoodItemDto>>($"/api/get-foods/{dateString}");

			if (savedFoods != null)
			{
				foreach (var food in savedFoods) FoodItems.Add(food);
				RecalculateTotals();
			}
		}
		catch (HttpRequestException)
		{
			await DisplayAlertAsync(AppResources.ConnectionError, AppResources.ServerUnreachable, AppResources.Ok);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync(AppResources.DataError, $"{AppResources.RecordsLoadError} {ex.Message}", AppResources.Ok);
		}
	}

	private void UpdateDateUI()
	{
		bool canGoBack = true; 
		PrevDayButton.IsEnabled = canGoBack;
		PrevDayButton.TextColor = canGoBack ? Color.FromArgb("#B900FF") : Color.FromArgb("#444444");
		
		bool canGoForward = _currentViewDate < DateTime.Today;
		NextDayButton.IsEnabled = canGoForward;
		NextDayButton.TextColor = canGoForward ? Color.FromArgb("#B900FF") : Color.FromArgb("#444444");

		if (_currentViewDate == DateTime.Today) DateLabel.Text = AppResources.Today;
		else if (_currentViewDate == DateTime.Today.AddDays(-1)) DateLabel.Text = AppResources.Yesterday;
		else DateLabel.Text = _currentViewDate.ToString("dd MMMM dddd").ToUpper();
	}

	private async void OnPrevDayClicked(object? sender, EventArgs e)
	{
		_currentViewDate = _currentViewDate.AddDays(-1);
		var currentClick = ++_dateChangeClickCount;
		await Task.Delay(300);
		if (currentClick == _dateChangeClickCount) await LoadFoodsForDate(_currentViewDate);
	}

	private async void OnNextDayClicked(object? sender, EventArgs e)
	{
		if (_currentViewDate.Date >= DateTime.Today) return;
		_currentViewDate = _currentViewDate.AddDays(1);
		
		var currentClick = ++_dateChangeClickCount;
		await Task.Delay(300);

		if (currentClick == _dateChangeClickCount)
		{
			await LoadFoodsForDate(_currentViewDate);
			MainDatePicker.Date = _currentViewDate;
		}
	}

	private void UpdateDynamicEffects(double totalCalories)
	{
		Color targetColor = GetTargetGlowColor(totalCalories);

		if (totalCalories >= 500 && GlowEffect.Opacity == 0) GlowEffect.FadeToAsync(0.8, 1000, Easing.CubicOut);
		else if (totalCalories < 500)
		{
			GlowEffect.FadeToAsync(0, 1000, Easing.CubicOut);
			_isGlowBreathing = false;
		}

		if (totalCalories >= 500) AnimateColorTransition(targetColor);
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
		if (calories > 1000 && calories <= 2000) return LerpColor(Color.FromArgb("#00FF00"), Color.FromArgb("#FFFF00"), (calories - 1000) / 1000.0);
		if (calories > 2000 && calories <= 3000) return LerpColor(Color.FromArgb("#FFFF00"), Color.FromArgb("#FF0000"), (calories - 2000) / 1000.0);
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

	private async void OnDateSelected(object sender, DateChangedEventArgs e)
	{
		if (!e.NewDate.HasValue || _currentViewDate.Date == e.NewDate.Value.Date) return;
		_currentViewDate = e.NewDate.Value.Date;
		await LoadFoodsForDate(_currentViewDate);
	}

	private async void OnSettingsClicked(object? sender, EventArgs e)
	{
		string action = await DisplayActionSheet("Language / Dil", "Cancel / İptal", null, "English", "Türkçe");

		if (action == "English" || action == "Türkçe")
		{
			string newLang = action == "English" ? "en" : "tr";
			Preferences.Default.Set("AppLanguage", newLang);
			
			if (Application.Current != null)
			{
				Application.Current.MainPage = new MainPage(_httpClientFactory);
			}
		}
	}
}