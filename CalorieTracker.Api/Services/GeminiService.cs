using System.Text.Json;
using CalorieTracker.Shared;

namespace CalorieTracker.Api.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeminiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<List<FoodItemDto>> AnalyzeFoodAsync(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            throw new ArgumentException("Please enter the food you ate.");

        if (userInput.Length > 150)
            throw new ArgumentException("Input is too long. Please summarize your meal in a maximum of 150 characters.");

        var sanitizedInput = userInput.Replace("\"", "").Replace("{", "").Replace("}", "").Trim();

        var apiKey = _configuration["Gemini:ApiKey"];
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent?key={apiKey}";

        var systemInstruction = @"You are a calorie analysis engine. Analyze the text provided by the user.
        Process only food-related items; categorize non-consumable items as Invalid Food.
        Round all numerical values to a maximum of 2 decimal places.
        Return an array STRICTLY in JSON format. Do not write any other explanations.
        Use these integers for MealType: 0 = Breakfast, 1 = Lunch, 2 = Dinner, 3 = Snack, 4 = UnknownMeal.
        If the user specifies a meal, assign the correct number; if not, assign 4.
        Example Output:
        [
        {
            ""OriginalQuery"": ""text"",
            ""FoodName"": ""Food name"",
            ""Portion"": ""Portion"",
            ""Calories"": 250.5,
            ""ProteinGrams"": 12.5,
            ""CarbsGrams"": 30.0,
            ""FatGrams"": 8.2,
            ""MealType"": 2, 
            ""IsFound"": true
        }
        ]";

        var requestBody = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = sanitizedInput } } }
            },
            generationConfig = new { responseMimeType = "application/json" }
        };

        var response = await _httpClient.PostAsJsonAsync(url, requestBody);

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            throw new Exception("Gemini API limit reached. Please wait 1 minute and try again.");
        }

        response.EnsureSuccessStatusCode();

        using var responseDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var jsonText = responseDoc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        if (string.IsNullOrWhiteSpace(jsonText)) return new List<FoodItemDto>();

        var result = JsonSerializer.Deserialize<List<FoodItemDto>>(jsonText, options) ?? new List<FoodItemDto>();

        foreach (var item in result)
        {
            if (item.Calories < 0 || item.Calories > 5000 ||
                item.ProteinGrams < 0 || item.ProteinGrams > 500 ||
                item.CarbsGrams < 0 || item.CarbsGrams > 500 ||
                item.FatGrams < 0 || item.FatGrams > 500)
            {
                throw new InvalidOperationException("Calculated values are outside physical limits. Please enter a clearer description.");
            }
        }

        return result;
    }
}