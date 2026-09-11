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
            throw new ArgumentException("Lütfen yediğiniz yemeği yazın.");

        if (userInput.Length > 150)
            throw new ArgumentException("Girdi çok uzun. Lütfen yemeğinizi maksimum 150 karakterle özetleyin.");

        var sanitizedInput = userInput.Replace("\"", "").Replace("{", "").Replace("}", "").Trim();

        var apiKey = _configuration["Gemini:ApiKey"];
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent?key={apiKey}";

        var systemInstruction = @"Sen bir kalori analiz motorusun. Kullanıcının verdiği metni incele.
        Sadece yiyecek olan metinleri işleme al, tüketilemeyen içerikleri Geçersiz Yiyecek diye gir.
        Tüm sayısal değerleri virgülden sonra en fazla 2 basamak olacak şekilde yuvarla.
        SADECE JSON formatında bir dizi döndür. Başka hiçbir açıklama yazma.
        MealType için şu tam sayıları kullan: 0 = Sabah, 1 = Ogle, 2 = Aksam, 3 = AraOgun, 4 = BilinmeyenOgun.
        Kullanıcı öğün belirtmişse doğru sayıyı ata, belirtmemişse 4 ata.
        Örnek Çıktı:
        [
        {
            ""OriginalQuery"": ""metin"",
            ""FoodName"": ""Yiyecek adı"",
            ""Portion"": ""Porsiyon"",
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
            throw new Exception("Gemini API sınırına ulaştı. Lütfen 1 dakika bekleyip tekrar deneyin.");
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
                throw new InvalidOperationException("Hesaplanan değerler fiziksel sınırların dışında. Lütfen daha net bir ifade girin.");
            }
        }

        return result;
    }
}