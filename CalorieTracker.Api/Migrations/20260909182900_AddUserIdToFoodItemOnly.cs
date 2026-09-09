using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalorieTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToFoodItemOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "FoodItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FoodItems_UserId_ConsumedDate",
                table: "FoodItems",
                columns: new[] { "UserId", "ConsumedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FoodItems_UserId_ConsumedDate",
                table: "FoodItems");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FoodItems");
        }
    }
}
