using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using CalorieTracker.Api;
using CalorieTracker.Api.Services;
using CalorieTracker.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("Jwt:Key ayarı bulunamadı. 'dotnet user-secrets set \"Jwt:Key\" \"...\"' ile ekleyin.");
var jwtIssuer = jwtSection["Issuer"] ?? "CalorieTracker.Api";
var jwtAudience = jwtSection["Audience"] ?? "CalorieTracker.Client";

var jwtExpireDays = double.TryParse(jwtSection["ExpireDays"], out var d) ? d : 1825;

builder.Services.AddHttpClient<GeminiService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=calories.db"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("GeminiLimit", context =>
    {
        var key = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.AddPolicy("WriteLimit", context =>
    {
        var key = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.AddPolicy("TokenLimit", context =>
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: key,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.RejectionStatusCode = 429;
});

var app = builder.Build();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/token", () =>
{

    var newUserId = Guid.NewGuid().ToString();

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, newUserId)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var expires = DateTime.UtcNow.AddDays(jwtExpireDays);

    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: expires,
        signingCredentials: creds
    );

    return Results.Ok(new AuthResponseDto
    {
        Token = new JwtSecurityTokenHandler().WriteToken(token),
        UserId = newUserId,
        ExpiresAtUtc = expires
    });
}).RequireRateLimiting("TokenLimit");


app.MapPost("/api/analyze-food", async (
    [FromBody] string userInput,
    [FromQuery] string? date,
    GeminiService geminiService,
    AppDbContext dbContext,
    ClaimsPrincipal principal,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("AnalyzeFood");
    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(userInput)) return Results.BadRequest("Boş olamaz.");

    var targetDate = DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
        ? parsed.Date
        : DateTime.Today;

    try
    {
        var result = await geminiService.AnalyzeFoodAsync(userInput);
        if (result != null && result.Count > 0)
        {
            foreach (var food in result)
            {
                food.ConsumedDate = targetDate;
                food.UserId = userId;
            }

            dbContext.FoodItems.AddRange(result);

            var thresholdDate = DateTime.Today.AddDays(-7);
            var oldRecords = dbContext.FoodItems
                .Where(f => f.UserId == userId && f.ConsumedDate < thresholdDate);
            dbContext.FoodItems.RemoveRange(oldRecords);

            await dbContext.SaveChangesAsync();
        }
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "AnalyzeFood işlemi başarısız oldu. UserId: {UserId}", userId);
        return Results.Problem("İstek işlenirken bir hata oluştu. Lütfen tekrar deneyin.", statusCode: 500);
    }
}).RequireRateLimiting("GeminiLimit").RequireAuthorization();

app.MapGet("/api/get-foods/{dateString}", async (
    string dateString,
    AppDbContext dbContext,
    ClaimsPrincipal principal) =>
{
    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Results.Unauthorized();

    if (!DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var targetDate))
        return Results.BadRequest($"Geçersiz tarih formatı: {dateString}");

    var foods = await dbContext.FoodItems
        .Where(f => f.UserId == userId && f.ConsumedDate == targetDate.Date)
        .ToListAsync();

    return Results.Ok(foods);
}).RequireAuthorization();

app.MapDelete("/api/delete-food/{id}", async (
    int id,
    AppDbContext dbContext,
    ClaimsPrincipal principal) =>
{
    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Results.Unauthorized();

    var food = await dbContext.FoodItems
        .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

    if (food == null) return Results.NotFound();

    dbContext.FoodItems.Remove(food);
    await dbContext.SaveChangesAsync();

    return Results.Ok();
}).RequireRateLimiting("WriteLimit").RequireAuthorization();

app.MapPut("/api/update-food/{id}", async (
    int id,
    [FromBody] FoodItemDto updatedFood,
    AppDbContext dbContext,
    ClaimsPrincipal principal) =>
{
    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Results.Unauthorized();

    if (updatedFood.Calories is < 0 or > 10000)
        return Results.BadRequest("Kalori 0 ile 10000 arasında olmalıdır.");
    if (updatedFood.ProteinGrams < 0 || updatedFood.CarbsGrams < 0 || updatedFood.FatGrams < 0)
        return Results.BadRequest("Makro değerleri negatif olamaz.");

    var food = await dbContext.FoodItems
        .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

    if (food == null) return Results.NotFound();

    food.Calories = updatedFood.Calories;
    food.ProteinGrams = updatedFood.ProteinGrams;
    food.CarbsGrams = updatedFood.CarbsGrams;
    food.FatGrams = updatedFood.FatGrams;
    food.Portion = updatedFood.Portion;
    food.MealType = updatedFood.MealType;

    await dbContext.SaveChangesAsync();

    return Results.Ok();
}).RequireRateLimiting("WriteLimit").RequireAuthorization();

app.Run();