using CalorieTracker.Api;
using CalorieTracker.Api.Services;
using CalorieTracker.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=calories.db"));
builder.Services.AddRateLimiter(options => {
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.RejectionStatusCode = 429;
});

var app = builder.Build();
app.UseRateLimiter();

app.MapPost("/api/analyze-food", async ([FromBody] string userInput, [FromQuery] string? date, GeminiService geminiService, AppDbContext dbContext) =>
{
    if (string.IsNullOrWhiteSpace(userInput)) return Results.BadRequest("Boş olamaz.");
    
    var targetDate = DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) 
        ? parsed.Date 
        : DateTime.Today;

    try
    {
        var result = await geminiService.AnalyzeFoodAsync(userInput);
        if (result != null && result.Count > 0)
        {
            foreach (var food in result) food.ConsumedDate = targetDate; 

            dbContext.FoodItems.AddRange(result);

            var thresholdDate = DateTime.Today.AddDays(-7);
            var oldRecords = dbContext.FoodItems.Where(f => f.ConsumedDate < thresholdDate);
            dbContext.FoodItems.RemoveRange(oldRecords);

            await dbContext.SaveChangesAsync();
        }
        return Results.Ok(result);
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

app.MapGet("/api/get-foods/{dateString}", async (string dateString, AppDbContext dbContext) =>
{
    if (!DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var targetDate)) 
        return Results.BadRequest($"Geçersiz tarih formatı: {dateString}");
    
    var foods = await dbContext.FoodItems.Where(f => f.ConsumedDate == targetDate.Date).ToListAsync();
    return Results.Ok(foods);
});

app.MapDelete("/api/delete-food/{id}", async (int id, AppDbContext dbContext) =>
{
    var food = await dbContext.FoodItems.FindAsync(id);
    if (food == null) return Results.NotFound();

    dbContext.FoodItems.Remove(food);
    await dbContext.SaveChangesAsync();
    
    return Results.Ok();
});

app.MapPut("/api/update-food/{id}", async (int id, [FromBody] FoodItemDto updatedFood, AppDbContext dbContext) =>
{
    var food = await dbContext.FoodItems.FindAsync(id);
    if (food == null) return Results.NotFound();

    food.Calories = updatedFood.Calories;
    food.ProteinGrams = updatedFood.ProteinGrams;
    food.CarbsGrams = updatedFood.CarbsGrams;
    food.FatGrams = updatedFood.FatGrams;
    food.Portion = updatedFood.Portion;
    food.MealType = updatedFood.MealType;

    await dbContext.SaveChangesAsync();
    
    return Results.Ok();
});

app.Run();