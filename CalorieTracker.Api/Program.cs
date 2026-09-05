using CalorieTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<GeminiService>();

var app = builder.Build();

app.MapPost("/api/analyze-food", async ([FromBody] string userInput, GeminiService geminiService) =>
{
    if (string.IsNullOrWhiteSpace(userInput))
        return Results.BadRequest("Yiyecek metni boş olamaz.");

    try
    {
        var result = await geminiService.AnalyzeFoodAsync(userInput);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Bir hata oluştu: {ex.Message}");
    }
});

app.Run();