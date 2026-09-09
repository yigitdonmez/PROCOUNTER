using CalorieTracker.Shared;
using Microsoft.EntityFrameworkCore;

namespace CalorieTracker.Api;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<FoodItemDto> FoodItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<FoodItemDto>()
            .HasIndex(f => new { f.UserId, f.ConsumedDate });
    }
}