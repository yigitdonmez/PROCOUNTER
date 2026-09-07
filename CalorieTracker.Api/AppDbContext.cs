using CalorieTracker.Shared;
using Microsoft.EntityFrameworkCore;

namespace CalorieTracker.Api;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<FoodItemDto> FoodItems { get; set; }
}